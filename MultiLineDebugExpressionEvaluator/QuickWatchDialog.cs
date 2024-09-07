using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MultiLineDebugExpressionEvaluator
{
    public sealed class QuickWatchDialog
    {
        public const string MultiLineExpressionTextBoxName = "MultiLineExpressionControl";

        private readonly Window _window;

        private readonly FrameworkElement? _originalExpressionComboBox;
        private readonly Grid? _expressionGrid;
        private readonly FrameworkElement? _originalExpressionTextBox;
        private readonly FrameworkElement? _expressionLabel;
        private readonly Button? _originalReevaluateButton;
        private readonly Grid? _reevaluateGrid;

        private readonly CheckBox _useMultiLineCheckBox;
        private readonly TextBox _multiLineExpressionTextBox;
        private readonly Button _newReevaluateButton;

        private bool _isOk =>
            _originalExpressionComboBox is not null
            && _expressionGrid is not null
            && _originalExpressionTextBox is not null
            && _expressionLabel is not null
            && _originalReevaluateButton is not null
            && _reevaluateGrid is not null
            ;

        private QuickWatchDialog(
            System.Windows.Window window
            )
        {
            if (window is null)
            {
                throw new ArgumentNullException(nameof(window));
            }

            _window = window;

            _originalExpressionComboBox = FindOriginalExpressionComboBox();
            _expressionGrid = _originalExpressionComboBox.Parent as Grid;
            _originalExpressionTextBox = FindOriginalExpressionTextBox();
            _expressionLabel = FindExpressionLabel();
            _originalReevaluateButton = FindReevaluateButton();
            _reevaluateGrid = _originalReevaluateButton.Parent as Grid;

            _useMultiLineCheckBox = new CheckBox
            {
                Content = "Use multiline evaluator (Ctrl+Enter is available to evaluate)",
                Margin = new Thickness(
                    30,
                    _expressionLabel.Margin.Top,
                    0,
                    _expressionLabel.Margin.Bottom
                    ),
                VerticalContentAlignment = VerticalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IsChecked = false
            };

            _multiLineExpressionTextBox = new System.Windows.Controls.TextBox
            {
                AcceptsReturn = true,
                Height = 100,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                Text = string.Empty,
                Margin = _originalExpressionComboBox.Margin,
                Name = MultiLineExpressionTextBoxName,
                Visibility = Visibility.Collapsed
            };
            _multiLineExpressionTextBox.KeyUp += MultiLineExpressionTextBox_KeyUp;

            _newReevaluateButton = new Button
            {
                Content = _originalReevaluateButton.Content,
                Margin = _originalReevaluateButton.Margin,
                Padding = _originalReevaluateButton.Padding,
                BorderThickness = _originalReevaluateButton.BorderThickness,
                MinHeight = _originalReevaluateButton.MinHeight
            };

        }

        public void InitialSetup()
        {
            if (!_isOk)
            {
                return;
            }

            var expressionComboBoxIndex = _expressionGrid.Children.IndexOf(
                _originalExpressionComboBox
                );

            var expressionText = GetText(_originalExpressionTextBox);
            var multiline = expressionText.Contains('\r') || expressionText.Contains('\n');

            #region use multiline checkbox

            var expressionLabelIndex = _expressionGrid.Children.IndexOf(
                _expressionLabel
                );
            _expressionGrid.Children.Remove(_expressionLabel);

            _useMultiLineCheckBox.IsChecked = multiline;
            _useMultiLineCheckBox.Checked += CheckBox_Status_Changed;
            _useMultiLineCheckBox.Unchecked += CheckBox_Status_Changed;

            var sp = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0),
                VerticalAlignment = VerticalAlignment.Bottom
            };
            Grid.SetRow(
                sp,
                Grid.GetRow(_expressionLabel)
                );
            sp.Children.Add(_expressionLabel);
            sp.Children.Add(_useMultiLineCheckBox);

            _expressionGrid.Children.Insert(expressionLabelIndex, sp);

            #endregion


            if (multiline)
            {
                _originalExpressionComboBox.Visibility = Visibility.Collapsed;
            }

            _multiLineExpressionTextBox.Text = expressionText;
            _multiLineExpressionTextBox.Visibility = multiline ? Visibility.Visible : Visibility.Collapsed;

            Grid.SetRow(
                _multiLineExpressionTextBox,
                Grid.GetRow(_originalExpressionComboBox)
                );

            _expressionGrid.Children.Insert(expressionComboBoxIndex, _multiLineExpressionTextBox);

            #region reevaluate button replace

            _newReevaluateButton.Click += NewReevaluateButton_Click;
            Grid.SetRow(
                _newReevaluateButton,
                Grid.GetRow(_originalReevaluateButton)
                );
            var reevaluateIndex = _reevaluateGrid.Children.IndexOf(_originalReevaluateButton);
            _reevaluateGrid.Children.Remove(_originalReevaluateButton);
            _reevaluateGrid.Children.Insert(reevaluateIndex, _newReevaluateButton);

            #endregion
        }

        private void NewReevaluateButton_Click(object sender, RoutedEventArgs e)
        {
            Reevaluate();
        }

        private void Reevaluate()
        {
            if (_useMultiLineCheckBox.IsChecked.GetValueOrDefault(false))
            {
                SetOriginalText(
                    _multiLineExpressionTextBox.Text
                    );
            }

            _originalReevaluateButton.RaiseEvent(
                new RoutedEventArgs(
                    System.Windows.Controls.Primitives.ButtonBase.ClickEvent
                    )
                );
        }

        public async Task WaitForClosingAsync(
            CancellationToken token
            )
        {
            if (!_isOk)
            {
                return;
            }

            while (!token.IsCancellationRequested)
            {
                var watchWindow = FindWatchWindow();
                if (watchWindow is null)
                {
                    return;
                }

                await Task.Delay(250);
            }
        }

        private static string GetText(
            FrameworkElement target
            )
        {
            var expressionTextBoxType = target.GetType();
            var textProperty = expressionTextBoxType.GetProperty(
                "Text",
                BindingFlags.Instance | BindingFlags.Public
                );
            if (textProperty is null)
            {
                return string.Empty;
            }
            return (string)textProperty.GetValue(
                target
                );
        }

        private void SetOriginalText(
            string text
            )
        {
            SetText(
                _originalExpressionTextBox,
                text
                );
        }

        private static void SetText(
            FrameworkElement target,
            string text
            )
        {
            var expressionTextBoxType = target.GetType();
            var textProperty = expressionTextBoxType.GetProperty(
                "Text",
                BindingFlags.Instance | BindingFlags.Public
                );
            if (textProperty is null)
            {
                return;
            }
            textProperty.SetValue(
                target,
                text
                );
        }

        private FrameworkElement? FindOriginalExpressionTextBox(
            )
        {
            var watchExpressionFrameworkElement = _window.GetRecursiveByName(
                "ExpressionTextBox"
                );
            if (watchExpressionFrameworkElement is null)
            {
                return null;
            }
            if (watchExpressionFrameworkElement.GetType().Name != "IntellisenseTextBox")
            {
                return null;
            }

            return watchExpressionFrameworkElement;
        }

        private FrameworkElement? FindOriginalExpressionComboBox(
            )
        {
            var expressionComboBox = _window.GetRecursiveByName(
                "ExpressionComboBox"
                );
            if (expressionComboBox is null)
            {
                return null;
            }

            return expressionComboBox;
        }

        private FrameworkElement? FindExpressionLabel(
            )
        {
            var expressionLabel = _window.GetRecursiveByName(
                "ExpressionLabel"
                );
            if (expressionLabel is null)
            {
                return null;
            }

            return expressionLabel;
        }

        private Button? FindReevaluateButton(
            )
        {
            List<DependencyObject> list = new();
            _window.GetAllRecursiveByPredicate(
                ref list,
                dobj =>
                    dobj is Button button
                    && button.Content is string
                );
            if (list.Count == 0)
            {
                return null;
            }

            var reevaluteButton = (
                from fe in list
                let button = fe as Button
                let point = button.PointToScreen(new Point(0, 0))
                orderby point.X descending
                orderby point.Y ascending
                select button
                ).FirstOrDefault();

            return reevaluteButton as Button;
        }

        private void CheckBox_Status_Changed(
            object sender,
            RoutedEventArgs e
            )
        {
            var checkBox = sender as CheckBox;

            if (checkBox.IsChecked.GetValueOrDefault(false))
            {
                _originalExpressionComboBox.Visibility = Visibility.Collapsed;
                _multiLineExpressionTextBox.Visibility = Visibility.Visible;
            }
            else
            {
                SetOriginalText(string.Empty);

                _originalExpressionComboBox.Visibility = Visibility.Visible;
                _multiLineExpressionTextBox.Visibility = Visibility.Collapsed;
            }
        }

        private void MultiLineExpressionTextBox_KeyUp(
            object sender,
            System.Windows.Input.KeyEventArgs e
            )
        {
            if (e.Key == System.Windows.Input.Key.Enter
                && (e.KeyboardDevice.Modifiers & ModifierKeys.Control) != 0)
            {
                Reevaluate();
            }
        }

        public static System.Windows.Window? FindWatchWindow(
            )
        {
            foreach (System.Windows.Window window in Application.Current.Windows)
            {
                if (window.GetType().Name == "QuickWatchDialog")
                {
                    return window;
                }
            }

            return null;
        }

        public static QuickWatchDialog? TryCreate()
        {
            var watchWindow = FindWatchWindow(
                );
            if (watchWindow is null)
            {
                return null;
            }

            return new QuickWatchDialog(
                watchWindow
                );
        }
    }
}
