using System.Windows;
using System.Windows.Controls;

namespace RailmlEditor.Views
{
    /// <summary>
    /// 화면 오른쪽(또는 어느 한 쪽)에 나타나는 '속성 편집창(Property Grid)' 화면을 담당합니다.
    /// 선택된 요소(선로, 신호기 등)의 세부 정보를 보여주고 수정할 수 있게 해줍니다.
    /// </summary>
    public partial class PropertiesView : UserControl
    {
        public PropertiesView()
        {
            InitializeComponent();
        }

        private void Properties_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            // Prevent property grid auto-scrolling weirdness if desired,
            // or let it handle it. Original code didn't seem to block it,
            // but usually preventing parents from scrolling when editing a child is good.
            // For now, let's allow it unless it causes issues.
            // e.Handled = true; 
        }
    }
}
