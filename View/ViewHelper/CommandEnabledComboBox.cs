﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Vulnerator.View.ViewHelper
{
    public class CommandEnabledComboBox : ComboBox, ICommandSource
    {
        private static EventHandler canExecuteChangedHandler;

        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
            "Command", typeof(ICommand), typeof(CommandEnabledComboBox), new PropertyMetadata((ICommand)null, new PropertyChangedCallback(CommandChanged)));

        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        public static readonly DependencyProperty CommandTargetProperty = DependencyProperty.Register(
            "CommandTarget", typeof(IInputElement), typeof(CommandEnabledComboBox), new PropertyMetadata((IInputElement)null));

        public IInputElement CommandTarget
        {
            get => (IInputElement)GetValue(CommandTargetProperty);
            set => SetValue(CommandTargetProperty, value);
        }

        public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(
            "CommandParameter", typeof(object), typeof(CommandEnabledComboBox), new PropertyMetadata((object)null));

        public object CommandParameter
        {
            get => (object)GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }

        public CommandEnabledComboBox() : base() { }

        private static void CommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CommandEnabledComboBox cb = (CommandEnabledComboBox)d;
            cb.HookUpCommand((ICommand)e.OldValue, (ICommand)e.NewValue);
        }

        private void HookUpCommand(ICommand oldCommand, ICommand newCommand)
        {
            if (oldCommand != null)
            {
                RemoveCommand(oldCommand, newCommand);
            }
            AddCommand(oldCommand, newCommand);
        }

        private void RemoveCommand(ICommand oldCommand, ICommand newCommand)
        {
            EventHandler handler = CanExecuteChanged;
            oldCommand.CanExecuteChanged -= handler;
        }

        private void AddCommand(ICommand oldCommand, ICommand newCommand)
        {
            EventHandler handler = new EventHandler(CanExecuteChanged);
            canExecuteChangedHandler = handler;
            if (newCommand != null)
            {
                newCommand.CanExecuteChanged += canExecuteChangedHandler;
            }
        }
        private void CanExecuteChanged(object sender, EventArgs e)
        {

            if (Command != null)
            {
                RoutedCommand command = Command as RoutedCommand;

                // If a RoutedCommand.
                if (command != null)
                {
                    if (command.CanExecute(CommandParameter, CommandTarget))
                    {
                        IsEnabled = true;
                    }
                    else
                    {
                        IsEnabled = false;
                    }
                }
                // If a not RoutedCommand.
                else
                {
                    if (Command.CanExecute(CommandParameter))
                    {
                        IsEnabled = true;
                    }
                    else
                    {
                        IsEnabled = false;
                    }
                }
            }
        }

        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);

            if (Command != null)
            {
                RoutedCommand command = Command as RoutedCommand;

                if (command != null)
                {
                    command.Execute(CommandParameter, CommandTarget);
                }
                else
                {
                    ((ICommand)Command).Execute(CommandParameter);
                }
            }
        }
    }
}
