namespace GoParkAPI
{
    public static class PlanData
    {
        // 使用 Dictionary 儲存方案資訊 (方案ID, (方案名稱, 價格))
        public static readonly Dictionary<string, (string Name, decimal Price)> Plans = new()
        {
            { "oneMonth", ("1個月方案", 3500) },
            { "threeMonths", ("3個月方案", 10200) },
            { "sixMonths", ("6個月方案", 19200) },
            { "twelveMonths", ("12個月方案", 36000) }
        };
    }
}
