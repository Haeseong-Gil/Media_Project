using System;

// 사용자가 정한 프로토타입 시험값. 최종 게임의 밸런스 확정을 뜻하지 않는다.
[Serializable]
public sealed class MinimalRunTuning
{
    public int startingPeople = 6;
    public double startingFood = 100;
    public double startingFuel = 1000;
    public double secondsPerDay = 300;
    public double foodPerPersonPerDay = 3;
    public double secondsPerFuelTick = 5;
    public double fuelPerTick = 1;
    public double departureFuelCost = 3;
    public double secondsPerZone = 60;
    public int minimumDispatch = 3;
    public int maximumDispatch = 10;
    public double secondsPerSearchTick = 3;
    public double foodPerExplorerTick = 2;
    public double fuelPerExplorerTick = 6;
    public double stationEfficiency = 4;
    public double baseRiskPercent = 0.5;
    public double riskPerExplorerPercent = 0.2;
    public double generalRiskIncrease = 0.2;
    public double stationRiskIncrease = 0.05;
    public double ninePersonBonus = 1.2;
    public double tenPersonBonus = 1.5;
    public int survivorsPerReturn = 5;

    public bool IsValid(out string error)
    {
        double[] values = { startingFood, startingFuel, secondsPerDay, foodPerPersonPerDay,
            secondsPerFuelTick, fuelPerTick, departureFuelCost, secondsPerZone, secondsPerSearchTick,
            foodPerExplorerTick, fuelPerExplorerTick, stationEfficiency, baseRiskPercent,
            riskPerExplorerPercent, generalRiskIncrease, stationRiskIncrease, ninePersonBonus, tenPersonBonus };
        foreach (double value in values)
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
            { error = "시험값은 유한한 0 이상의 값이어야 합니다."; return false; }
        if (startingPeople < 1 || minimumDispatch < 3 || maximumDispatch < minimumDispatch || maximumDispatch > 10 ||
            survivorsPerReturn < 1 || secondsPerDay < 0.01 || secondsPerFuelTick < 0.01 ||
            secondsPerZone < 0.01 || secondsPerSearchTick < 0.01 || fuelPerTick <= 0 || stationEfficiency <= 0)
        { error = "인원·주기·효율 설정을 확인하세요. 파견 범위는 3~10명 안이어야 합니다."; return false; }
        error = null; return true;
    }
}

// UI와 독립된 규칙. 난수 공급자를 주입할 수 있어 확률 경로를 재현해 검증할 수 있다.
public sealed class MinimalRunModel
{
    private const double Epsilon = 0.0000001;
    private readonly Func<double> random01;
    public readonly MinimalRunTuning Tuning;
    public bool Started { get; private set; }
    public bool Failed { get; private set; }
    public bool Moving { get; private set; }
    public bool AtStation { get; private set; }
    public bool TeamAtStation { get; private set; }
    public bool IsHungry { get; private set; }
    public bool SurvivorEventOccurred { get; private set; }
    public int People { get; private set; }
    public int Team { get; private set; }
    public int PendingSurvivors { get; private set; }
    public int HungryDays { get; private set; }
    public int Day { get; private set; } = 1;
    public double Food { get; private set; }
    public double Fuel { get; private set; }
    public double CarriedFood { get; private set; }
    public double CarriedFuel { get; private set; }
    public double DayElapsed { get; private set; }
    public double ZoneElapsed { get; private set; }
    public double FuelTickElapsed { get; private set; }
    public double SearchTickElapsed { get; private set; }
    public double RiskIncreasePercent { get; private set; }
    public double Elapsed { get; private set; }
    public string Report { get; private set; } = "타이틀에서 시작을 누르면 새 RUN이 시작됩니다.";
    public bool ChoicePaused => PendingSurvivors > 0;
    public bool CanAct => Started && !Failed && !ChoicePaused;
    public int AvailablePeople => People - Team;
    public int MaximumSelectable => Math.Min(Tuning.maximumDispatch, People - 1);
    public double CurrentRiskPercent => Team > 0 ? Math.Min(100, Tuning.baseRiskPercent + Team * Tuning.riskPerExplorerPercent + RiskIncreasePercent) : 0;
    public double CurrentBonus => Team == 10 ? Tuning.tenPersonBonus : Team == 9 ? Tuning.ninePersonBonus : 1;
    public int CurrentLossCount => LossCount(Team);

    public MinimalRunModel(MinimalRunTuning tuning, Func<double> randomSource = null)
    {
        if (tuning == null) throw new ArgumentNullException(nameof(tuning));
        if (!tuning.IsValid(out string error)) throw new ArgumentException(error);
        Tuning = tuning;
        var random = new Random();
        random01 = randomSource ?? random.NextDouble;
        People = tuning.startingPeople; Food = tuning.startingFood; Fuel = tuning.startingFuel;
    }

    public void Start()
    {
        if (Started) return;
        Started = true;
        Report = "일반 구역에 정차했습니다. 출발하거나 인원을 선택해 탐사를 파견하세요.";
    }

    public void ToggleDriving()
    {
        if (!CanAct) return;
        if (Moving) { Moving = false; Report = "정차했습니다. 연료 소비 주기도 정지합니다."; return; }
        if (Team > 0) { Report = "탐사대가 귀환해야 출발할 수 있습니다."; return; }
        if (Fuel + Epsilon < Tuning.departureFuelCost || Fuel <= Epsilon)
        { Report = $"출발에 연료 {Tuning.departureFuelCost:0.#}이 필요합니다."; return; }
        Fuel = Math.Max(0, Fuel - Tuning.departureFuelCost);
        Moving = Fuel > Epsilon;
        Report = Moving ? $"출발했습니다. 연료 {Tuning.departureFuelCost:0.#} 소비. 이후 운행 {Tuning.secondsPerFuelTick:0.#}초마다 {Tuning.fuelPerTick:0.#} 소비합니다."
            : "출발 비용으로 연료가 고갈되어 즉시 정차했습니다.";
    }

    public bool Dispatch(int count)
    {
        if (!CanAct || Moving || Team > 0 || count < Tuning.minimumDispatch || count > MaximumSelectable) return false;
        Team = count; TeamAtStation = AtStation;
        CarriedFood = CarriedFuel = SearchTickElapsed = RiskIncreasePercent = 0;
        Report = $"{count}명을 {(AtStation ? "폐역" : "일반 구역")}에 파견했습니다. {Tuning.secondsPerSearchTick:0.#}초마다 확보·위험 판정을 합니다.";
        return true;
    }

    public void Return()
    {
        if (!CanAct || Team == 0) return;
        CompleteReturn(false, "");
    }

    private void CompleteReturn(bool forced, string prefix)
    {
        if (Team == 0) return;
        bool stationReturn = TeamAtStation;
        string result = $"{(forced ? "강제 귀환" : "귀환")} {Team}명. 식량 {CarriedFood:0.0}, 연료 {CarriedFuel:0.0} 입고.";
        Food += CarriedFood; Fuel += CarriedFuel;
        Team = 0;
        CarriedFood = CarriedFuel = SearchTickElapsed = RiskIncreasePercent = 0;
        if (stationReturn && !SurvivorEventOccurred)
        {
            SurvivorEventOccurred = true;
            PendingSurvivors = Tuning.survivorsPerReturn;
            result += $" 생존자 {PendingSurvivors}명의 수용을 결정하세요. 시간이 정지했습니다.";
        }
        Report = prefix + result;
    }

    public void DecideSurvivors(bool accept)
    {
        if (!Started || Failed || PendingSurvivors == 0) return;
        int count = PendingSurvivors;
        PendingSurvivors = 0;
        if (accept) People += count;
        Report = accept ? $"생존자 {count}명을 수용했습니다. 다음 배급 필요량은 {People * Tuning.foodPerPersonPerDay:0.#}입니다. 시간 진행을 재개합니다."
            : $"생존자 {count}명의 합류를 거절했습니다. 인원은 그대로이며 시간 진행을 재개합니다.";
    }

    public void Advance(double seconds)
    {
        if (!CanAct || seconds <= 0 || double.IsNaN(seconds) || double.IsInfinity(seconds)) return;
        double remaining = seconds;
        while (remaining > Epsilon && CanAct)
        {
            double slice = Math.Min(remaining, Tuning.secondsPerDay - DayElapsed);
            if (Moving)
            {
                slice = Math.Min(slice, Tuning.secondsPerZone - ZoneElapsed);
                slice = Math.Min(slice, Tuning.secondsPerFuelTick - FuelTickElapsed);
            }
            if (Team > 0) slice = Math.Min(slice, Tuning.secondsPerSearchTick - SearchTickElapsed);
            if (Moving) { FuelTickElapsed += slice; ZoneElapsed += slice; }
            if (Team > 0) SearchTickElapsed += slice;
            DayElapsed += slice; Elapsed += slice; remaining -= slice;

            // 같은 시점의 처리 순서: 구역 → 연료 → 탐사 획득·손실·귀환 → 하루 배급.
            if (Moving && ZoneElapsed >= Tuning.secondsPerZone - Epsilon)
            {
                ZoneElapsed = 0; AtStation = !AtStation;
                Report = AtStation ? $"폐역에 진입했습니다. 정차 후 탐사하면 일반 구역의 {Tuning.stationEfficiency:0.#}배를 확보합니다."
                    : "일반 구역에 진입했습니다.";
            }
            if (Moving && FuelTickElapsed >= Tuning.secondsPerFuelTick - Epsilon)
            {
                FuelTickElapsed = 0;
                Fuel = Math.Max(0, Fuel - Tuning.fuelPerTick);
                if (Fuel <= Epsilon) { Fuel = 0; Moving = false; Report = "연료가 고갈되어 자동 정차했습니다. 탐사로 보충한 뒤 출발하세요."; }
            }
            if (Team > 0 && SearchTickElapsed >= Tuning.secondsPerSearchTick - Epsilon)
            { SearchTickElapsed = 0; SearchTick(); }
            if (DayElapsed >= Tuning.secondsPerDay - Epsilon)
            { DayElapsed = 0; Day++; DailyRation(); }
        }
    }

    private void SearchTick()
    {
        double efficiency = (TeamAtStation ? Tuning.stationEfficiency : 1) * CurrentBonus;
        CarriedFood += Team * Tuning.foodPerExplorerTick * efficiency;
        CarriedFuel += Team * Tuning.fuelPerExplorerTick * efficiency;
        if (random01() < CurrentRiskPercent / 100)
        {
            int before = Team;
            int lost = LossCount(before);
            int charged = before == 10 ? 5 : lost;
            double share = Math.Max(0, 1 - (double)charged / before);
            Team -= lost; People -= lost;
            CarriedFood *= share; CarriedFuel *= share;
            RiskIncreasePercent = 0;
            string loss = $"탐사자 {lost}명 손실. 운반 자원 {charged}명분 차감. ";
            Report = loss + $"남은 {Team}명 기준 위험도로 초기화했습니다.";
            if (Team <= 2) CompleteReturn(true, loss);
        }
        else RiskIncreasePercent += TeamAtStation ? Tuning.stationRiskIncrease : Tuning.generalRiskIncrease;
    }

    private void DailyRation()
    {
        double required = People * Tuning.foodPerPersonPerDay;
        bool enough = Food + Epsilon >= required;
        Food = Math.Max(0, Food - required);
        if (enough) { IsHungry = false; HungryDays = 0; return; }
        if (!IsHungry) { IsHungry = true; HungryDays = 0; }
        else HungryDays++;
        if (HungryDays >= 7)
        {
            Failed = true; Moving = false; People = Team = PendingSurvivors = 0;
            CarriedFood = CarriedFuel = 0;
            Report = "굶주림 7일차에 도달해 공동체 전원이 사망했습니다. RUN이 종료되었습니다.";
        }
        else Report = $"하루 배급이 부족했습니다. 굶주림 {HungryDays}일차입니다.";
    }

    private static int LossCount(int count) => count >= 10 ? 4 : count == 9 ? 3 : count >= 6 ? 2 : count > 0 ? 1 : 0;
}
