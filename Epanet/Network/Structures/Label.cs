namespace Epanet.Network.Structures {

    ///<summary>Text label</summary>
    public class Label {
        public enum MeterTypes {
            None,
            Node,
            Link
        }

        public Label(string text) {
            Text = text;
            Position = EnPoint.Invalid;
        }

        ///<summary>Label position.</summary>
        public EnPoint Position { get; set; }

        ///<summary>Label text.</summary>
        public string Text { get; set; }
        public Node Anchor { get; set; }
        public string FontName { get; set; }
        public int FontSize { get; set; }
        public bool FontBold { get; set; }
        public bool FontItalic { get; set; }
        public MeterTypes MeterType { get; set; }
        public string MeterId { get; set; }
    }

}
