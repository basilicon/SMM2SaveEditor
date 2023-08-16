using Avalonia.Controls;

namespace SMM2Level
{
    public partial class EntityEditor : UserControl
    {
        public static EntityEditor? Instance { get; private set; }

        public EntityEditor()
        {
            Instance = this;
            InitializeComponent();
        }
    }
}
