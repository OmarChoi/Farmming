using UnityEngine;

// SkyboxController 및 DirectionalLightController에서 공유하는 시간 세그먼트 계산 유틸리티
// [0 ~ dayStart]    : Night → Dawn
// [dayStart ~ sunrise] : Dawn → Day
// [sunrise ~ sunset]   : Day → Dusk
// [sunset ~ dayEnd]    : Dusk → Night
// [dayEnd ~ 24:00]     : Night → Night
public static class SkySegmentResolver
{
    // 현재 시간이 속한 세그먼트의 시작/끝 키프레임과 구간 내 선형 진행도를 계산한다
    public static bool TryResolve(
        GameTime time,
        TimeSettingSO settings,
        SkyDatabase database,
        out SkyKeyframeSO from,
        out SkyKeyframeSO to,
        out float progress)
    {
        from = null;
        to = null;
        progress = 0f;

        if (settings == null || database == null) return false;

        int dayStart = settings.DayStartTime.TotalMinutes;
        int sunrise = settings.SunriseTime.TotalMinutes;
        int sunset = settings.SunsetTime.TotalMinutes;
        int dayEnd = settings.DayEndTime.TotalMinutes;
        int current = time.TotalMinutes;

        // 시간 경계 순서가 올바르지 않으면 갱신하지 않음
        if (dayStart > sunrise || sunrise > sunset || sunset > dayEnd || dayEnd > GameTime.MinutesPerDay) return false;

        if (TryGetLinearProgress(current, 0, dayStart, out progress))
        {
            from = database.Get(ESkyType.Night);
            to = database.Get(ESkyType.Dawn);
            return true;
        }

        if (TryGetLinearProgress(current, dayStart, sunrise, out progress))
        {
            from = database.Get(ESkyType.Dawn);
            to = database.Get(ESkyType.Day);
            return true;
        }

        if (TryGetLinearProgress(current, sunrise, sunset, out progress))
        {
            from = database.Get(ESkyType.Day);
            to = database.Get(ESkyType.Dusk);
            return true;
        }

        if (TryGetLinearProgress(current, sunset, dayEnd, out progress))
        {
            from = database.Get(ESkyType.Dusk);
            to = database.Get(ESkyType.Night);
            return true;
        }

        // dayEnd 이후는 밤
        from = database.Get(ESkyType.Night);
        to = database.Get(ESkyType.Night);
        return true;
    }

    // AnimationCurve를 적용하되, 키가 없으면 선형 값을 그대로 사용한다
    public static float EvaluateCurve(AnimationCurve curve, float value)
        => Mathf.Clamp01(curve is { length: > 0 } ? curve.Evaluate(value) : value);

    // current가 [start, end) 범위에 있으면 구간 내 선형 진행도(0~1)를 계산한다
    private static bool TryGetLinearProgress(int current, int start, int end, out float progress)
    {
        progress = 0f;
        if (end <= start || current < start || current >= end) return false;
        progress = (current - start) / (float)(end - start);
        return true;
    }
}
