using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using SMM2SaveEditor.Utility;
using SMM2SaveEditor.Utility.EditorHelpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace SMM2SaveEditor.Utility.EditorHelpers
{
    public partial class EntityEditor : UserControl
    {
        public static EntityEditor? Instance { get; set; }

        private Entity? objRef = null;

        private StackPanel editorStack;

        public EntityEditor()
        {
            Instance = this;
            InitializeComponent();

            editorStack = this.Find<StackPanel>("EditorStack")!;
        }

        public void OpenOptions(Entity entity)
        {
            editorStack.Children.RemoveAll(editorStack.Children);
            GC.Collect();
            GC.WaitForPendingFinalizers();

            objRef = entity;

            foreach (Type t in entity.GetType().GetInheritanceHierarchy())
            {
                ObjectEditor objectEditor = new();
                editorStack.Children.Add(objectEditor);
                objectEditor.OpenOptions((Convert.ChangeType(entity, t) as Entity)!);

                Debug.WriteLine(t.Name);
            }

        }
    }
}
