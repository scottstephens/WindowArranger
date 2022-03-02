namespace WindowArranger
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();

            this.Load += Form1_Load;
        }

        private void Form1_Load(object? sender, EventArgs e)
        {
            var p = new ConsoleProgram();
            p.Run(new string[] { });
            this.Close();
        }
    }
}