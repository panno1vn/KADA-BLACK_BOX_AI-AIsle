using System.Windows.Controls;
using System.Windows.Input;
using AIsle.DesktopApp.ViewModels;

namespace AIsle.DesktopApp.Views 
{ 
    public partial class LayoutView : UserControl 
    { 
        public LayoutView() 
        { 
            InitializeComponent(); 
        } 

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && DataContext is LayoutViewModel vm)
            {
                var canvas = (Canvas)sender;
                var pos = e.GetPosition(canvas);
                vm.HandlePointerDown(pos.X, pos.Y);
                canvas.CaptureMouse();
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (DataContext is LayoutViewModel vm)
            {
                var canvas = (Canvas)sender;
                var pos = e.GetPosition(canvas);
                vm.HandlePointerMove(pos.X, pos.Y);
            }
        }

        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is LayoutViewModel vm)
            {
                var canvas = (Canvas)sender;
                var pos = e.GetPosition(canvas);
                vm.HandlePointerUp(pos.X, pos.Y);
                canvas.ReleaseMouseCapture();
            }
        }
    } 
}
