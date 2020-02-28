namespace Orchestrion.Components
{
    public class TopmostMessageBox
    {
        public static void Show(string text)
        {
            using (var form = new System.Windows.Forms.Form { TopMost = true })
            {
                System.Windows.Forms.MessageBox.Show(form, text);
            }
        }
    }
}
