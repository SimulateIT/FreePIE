using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using FreePIE.GUI.CodeCompletion.Controls;

namespace FreePIE.GUI.CodeCompletion
{
    public partial class CompletionPopupView
    {
        public CompletionPopupView()
        {
            this.DisplayActions = new PopupActionList();
            this.ListBoxActions = new PopupActionList();
            InitializeComponent();
            CompletionElements.PreviewKeyDown += (sender, args) =>
                                                     {
                                                         CheckForTriggeredKeyActions(DisplayActions, args);
                                                         CheckForElementInsertion(args);
                                                     };

            CompletionElements.ItemClicked += CompletionElementsItemClicked;
        }

        void CompletionElementsItemClicked(object sender, EventArgs<object, MouseButtonEventArgs> e)
        {
            var completionItem = (e.Arg1 as ICompletionItem);

            if(completionItem == null)
                throw new InvalidOperationException("ICompletionItem is null. Something is wrong with the hackish ItemClicked event.");

            InsertItem(completionItem);

            e.Arg2.Handled = true;
        }

        private void CheckForElementInsertion(KeyEventArgs args)
        {
            if (CompletionElements.SelectedItem == null || args.Key != Key.Enter)
                return;

            InsertItem(CompletionElements.SelectedItem as ICompletionItem);

            args.Handled = true;
        }

        private void InsertItem(ICompletionItem item)
        {
            item.Insert();
            PopupViewActions.Hide(this);
        }

        [TypeConverter(typeof(EditorAdapterConverter))]
        public EditorAdapterBase Target
        {
            get { return (EditorAdapterBase)GetValue(TargetProperty); }
            set { SetValue(TargetProperty, value); }
        }

        [TypeConverter(typeof(EditorAdapterConverter))]
        public static readonly DependencyProperty TargetProperty =
            DependencyProperty.Register("Target", typeof (EditorAdapterBase), typeof (CompletionPopupView), new PropertyMetadata(default(EditorAdapterBase), OnTargetChanged));

        private static void OnTargetChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue == null || obj == null)
                return;

            EditorAdapterBase target = e.NewValue as EditorAdapterBase;
            CompletionPopupView view = obj as CompletionPopupView;
            
            EventHandler selectionChanged = (sender, args) => view.OnEditorSelectionChanged();
            KeyEventHandler keyDown = (sender, args) => view.CheckForTriggeredKeyActions(view.CurrentActions, args);

            if (e.NewValue != null)
            {
                EditorAdapterBase editorAdapterBase = e.NewValue as EditorAdapterBase;
                editorAdapterBase.SelectionChanged += selectionChanged;
                editorAdapterBase.PreviewKeyDown += keyDown;
            }

            if (e.OldValue != null)
            {
                EditorAdapterBase editorAdapterBase = e.OldValue as EditorAdapterBase;
                editorAdapterBase.SelectionChanged -= selectionChanged;
                editorAdapterBase.PreviewKeyDown -= keyDown;
            }
        }

        public void PerformElementChanged(KeyEventArgs args)
        {
            if (CompletionElements.Items.Count <= 0) 
                return;

            CompletionElements.Focus();
            (CompletionElements.ItemContainerGenerator.ContainerFromIndex(0) as UIElement).Focus();
            CompletionElements.RaiseEvent(args);
        }

        private void CheckForTriggeredKeyActions(IEnumerable<IPopupAction> actions, KeyEventArgs args)
        {
            CheckForTriggeredActions(EventType.KeyPress, args, actions);
        }

        private void CheckForTriggeredActions(EventType type, object args, IEnumerable<IPopupAction> actions)
        {
            foreach(IPopupAction action in actions)
                action.Act(type, this, args);
        }

        private void OnEditorSelectionChanged()
        {
            UpdatePlacementRectangle();
            CheckForTriggeredActions(EventType.SelectionChanged, Target.CaretIndex, CurrentActions);
        }

        private IEnumerable<IPopupAction> CurrentActions
        {
            get{ return IsOpen ? DisplayActions.Union(ListBoxActions) : DisplayActions; }
        }

        private void UpdatePlacementRectangle()
        {
            var binding = BindingOperations.GetMultiBindingExpression(this, PlacementRectangleProperty);
            if (binding != null)
                binding.UpdateTarget();
        }

        public static readonly DependencyProperty KeyActionsProperty =
            DependencyProperty.Register("DisplayActions", typeof(PopupActionList), typeof(CompletionPopupView), new PropertyMetadata(null));

        public static readonly DependencyProperty ListBoxActionsProperty =
            DependencyProperty.Register("ListBoxActions", typeof (PopupActionList), typeof (CompletionPopupView), new PropertyMetadata(default(PopupActionList)));

        public PopupActionList ListBoxActions
        {
            get { return (PopupActionList) GetValue(ListBoxActionsProperty); }
            set { SetValue(ListBoxActionsProperty, value); }
        }

        public PopupActionList DisplayActions
        {
            get { return (PopupActionList)GetValue(KeyActionsProperty); }
            set { SetValue(KeyActionsProperty, value); }
        }

        public class PopupActionList : ObservableCollection<IPopupAction>
        { }
    }
}