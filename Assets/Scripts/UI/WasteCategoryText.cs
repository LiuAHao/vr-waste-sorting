public static class WasteCategoryText
{
    public static string Format(WasteCategory category)
    {
        switch (category)
        {
            case WasteCategory.Recyclable:
                return "可回收物";
            case WasteCategory.Hazardous:
                return "有害垃圾";
            case WasteCategory.Kitchen:
                return "厨余垃圾";
            default:
                return "其他垃圾";
        }
    }
}
