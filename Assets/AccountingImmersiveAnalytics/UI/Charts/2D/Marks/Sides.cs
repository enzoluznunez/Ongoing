namespace AccountingImmersiveAnalytics.UI.Charts.TwoD.Marks
{
    public struct Sides
    {
        public Side Left, Top, Right, Bottom;

        public Sides(Side left, Side top, Side right, Side bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }
    }
}
