using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using static QuizPractice.Models;

namespace QuizPracticeLinux.Views
{
    public partial class MainWindow : Window
    {
        private List<Question> questions;
        private int currentIndex = 0;
        private Dictionary<int, List<int>> userAnswers = new();
        private bool quizCompleted = false;
        private string selectedQuestionsFile;

        public MainWindow()
        {
            InitializeComponent();
            PopulateQuestionsFileComboBox();
            LoadQuestions();
            ApplySettingsAndStart();
        }

        private string GetQuestionsDirectory()
        {
#if DEBUG
            return @"c:\aa";
#else
            return AppContext.BaseDirectory;
#endif
        }

        private void PopulateQuestionsFileComboBox()
        {
            var directory = GetQuestionsDirectory();
            if (Directory.Exists(directory))
            {
                var files = Directory.GetFiles(directory, "questions_output_*.json")
                                     .OrderBy(f => f)
                                     .ToList();
                QuestionsFileComboBox.ItemsSource = files;
                if (files.Count > 0)
                {
                    QuestionsFileComboBox.SelectedIndex = 0;
                    selectedQuestionsFile = files[0];
                }
            }
        }

        private void QuestionsFileComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (QuestionsFileComboBox.SelectedItem is string file)
            {
                selectedQuestionsFile = file;
                LoadQuestions();
                ApplySettingsAndStart();
            }
        }

        private void LoadQuestions()
        {
            var directory = GetQuestionsDirectory();
            var path = selectedQuestionsFile ?? Path.Combine(directory, "questions_output.json");
            if (File.Exists(path))
            {
                questions = JsonSerializer.Deserialize<List<Question>>(File.ReadAllText(path)) ?? new();
            }
            else
            {
                questions = new();
                _ = ShowMessageBox("Questions file not found.");
            }
        }

        private void ApplySettingsAndStart()
        {
            if (RandomizeQuestionsCheckBox.IsChecked == true)
                questions = questions.OrderBy(q => Guid.NewGuid()).ToList();

            if (RandomizeAnswersCheckBox.IsChecked == true)
                foreach (var q in questions)
                    q.Options = q.Options.OrderBy(a => Guid.NewGuid()).ToList();

            currentIndex = 0;
            userAnswers.Clear();
            quizCompleted = false;
            ShowQuestion();
        }

        private void ShowQuestion()
        {
            if (questions == null || questions.Count == 0) return;

            var q = questions[currentIndex];

            QuestionText.Text = $"Q{currentIndex + 1}: {q.Text}";

            AnswersPanel.Children.Clear();
            FeedbackText.IsVisible = false;

            bool isSingleChoice = q.DataType == "single" || q.DataType == "singleChoice";

            for (int i = 0; i < q.Options.Count; i++)
            {
                var opt = q.Options[i];
                var answerText = new TextBlock
                {
                    Text = opt.Text,
                    TextWrapping = TextWrapping.Wrap,
                    Width = 500
                };

                Control answerControl;
                if (isSingleChoice)
                {
                    answerControl = new RadioButton
                    {
                        Content = answerText,
                        GroupName = "Answers",
                        Tag = i,
                        Margin = new Thickness(4, 4, 0, 0)
                    };
                    ((RadioButton)answerControl).Checked += AnswerSelected;
                    ((RadioButton)answerControl).Unchecked += AnswerSelected;
                }
                else
                {
                    answerControl = new CheckBox
                    {
                        Content = answerText,
                        Tag = i,
                        Margin = new Thickness(4, 4, 0, 0)
                    };
                    ((CheckBox)answerControl).Checked += AnswerSelected;
                    ((CheckBox)answerControl).Unchecked += AnswerSelected;
                }

                var border = new Border
                {
                    Child = answerControl,
                    Background = Brushes.Transparent,
                    CornerRadius = new CornerRadius(4),
                    Margin = new Thickness(0, 2, 0, 2)
                };

                AnswersPanel.Children.Add(border);
            }

            // Restore previous selection if any
            if (userAnswers.TryGetValue(currentIndex, out var selected))
            {
                for (int i = 0; i < AnswersPanel.Children.Count; i++)
                {
                    if (AnswersPanel.Children[i] is Border border)
                    {
                        if (border.Child is RadioButton rb)
                            rb.IsChecked = selected.Contains(i);
                        else if (border.Child is CheckBox cb)
                            cb.IsChecked = selected.Contains(i);
                    }
                }
            }

            // Show feedback if quiz is completed
            if (quizCompleted)
            {
                ShowFeedback();
            }
        }

        private void AnswerSelected(object? sender, RoutedEventArgs e)
        {
            var selectedIndices = new List<int>();
            for (int i = 0; i < AnswersPanel.Children.Count; i++)
            {
                if (AnswersPanel.Children[i] is Border border)
                {
                    if (border.Child is RadioButton rb && rb.IsChecked == true)
                        selectedIndices.Add(i);
                    else if (border.Child is CheckBox cb && cb.IsChecked == true)
                        selectedIndices.Add(i);
                }
            }
            userAnswers[currentIndex] = selectedIndices;
        }

        private void ShowFeedback()
        {
            var q = questions[currentIndex];
            if (!userAnswers.TryGetValue(currentIndex, out var selected)) return;

            var correctIndices = q.Options
                .Select((opt, idx) => opt.IsCorrect == 1 ? idx : -1)
                .Where(idx => idx != -1)
                .ToList();

            bool isCorrect = selected.Count == correctIndices.Count && !selected.Except(correctIndices).Any();

            FeedbackText.Text = isCorrect
                ? (q.wpProQuiz_correct ?? "Correct!")
                : (q.wpProQuiz_incorrect ?? "Incorrect.");

            FeedbackText.IsVisible = true;

            // Highlight answers
            for (int i = 0; i < AnswersPanel.Children.Count; i++)
            {
                var isCorrectAnswer = correctIndices.Contains(i);
                IBrush color = Brushes.Transparent;

                if (AnswersPanel.Children[i] is Border border)
                {
                    bool isSelected = false;
                    if (border.Child is RadioButton rb)
                        isSelected = rb.IsChecked == true;
                    else if (border.Child is CheckBox cb)
                        isSelected = cb.IsChecked == true;

                    if (isSelected && isCorrectAnswer)
                        color = new SolidColorBrush(Color.FromArgb(128, 50, 205, 50)); // light green
                    else if (isSelected && !isCorrectAnswer)
                        color = new SolidColorBrush(Color.FromArgb(128, 255, 99, 71)); // light red
                    else if (!isSelected && isCorrectAnswer)
                        color = new SolidColorBrush(Color.FromArgb(64, 50, 205, 50)); // faint green for missed correct

                    border.Background = color;
                }
            }
        }

        private void NextButton_Click(object? sender, RoutedEventArgs e)
        {
            if (currentIndex < questions.Count - 1)
            {
                currentIndex++;
                ShowQuestion();
            }
            else
            {
                ShowResults();
            }
        }

        private void PrevButton_Click(object? sender, RoutedEventArgs e)
        {
            if (currentIndex > 0)
            {
                currentIndex--;
                ShowQuestion();
            }
        }

        private void ShowResults()
        {
            int correctCount = 0;
            for (int i = 0; i < questions.Count; i++)
            {
                var q = questions[i];
                if (!userAnswers.TryGetValue(i, out var selected)) continue;

                var correctIndices = q.Options
                    .Select((opt, idx) => opt.IsCorrect == 1 ? idx : -1)
                    .Where(idx => idx != -1)
                    .ToList();

                bool isCorrect = selected.Count == correctIndices.Count && !selected.Except(correctIndices).Any();
                if (isCorrect) correctCount++;
            }

            _ = ShowMessageBox($"Quiz complete!\nScore: {correctCount} / {questions.Count}", "Results");
            quizCompleted = true;
            currentIndex = 0;
            ShowQuestion();
        }

        private void RandomizeSettingChanged(object? sender, RoutedEventArgs e)
        {
            ApplySettingsAndStart();
        }

        private void ShowFeedbackButton_Click(object? sender, RoutedEventArgs e)
        {
            ShowFeedback();
        }

        private void RestartButton_Click(object? sender, RoutedEventArgs e)
        {
            ApplySettingsAndStart();
        }

        private async void SolveButton_Click(object? sender, RoutedEventArgs e)
        {
            var q = questions[currentIndex];
            var correctIndices = q.Options
                .Select((opt, idx) => opt.IsCorrect == 1 ? idx : -1)
                .Where(idx => idx != -1)
                .ToList();

            // Temporarily detach event handlers
            for (int i = 0; i < AnswersPanel.Children.Count; i++)
            {
                if (AnswersPanel.Children[i] is Border border)
                {
                    if (border.Child is RadioButton rb)
                        rb.IsCheckedChanged -= AnswerSelected;
                    else if (border.Child is CheckBox cb)
                        cb.IsCheckedChanged -= AnswerSelected;
                }
            }

            // Set correct answers in UI
            for (int i = 0; i < AnswersPanel.Children.Count; i++)
            {
                if (AnswersPanel.Children[i] is Border border)
                {
                    if (border.Child is RadioButton rb)
                        rb.IsChecked = correctIndices.Contains(i);
                    else if (border.Child is CheckBox cb)
                        cb.IsChecked = correctIndices.Contains(i);
                }
            }

            // Update userAnswers directly
            userAnswers[currentIndex] = correctIndices;

            // Reattach event handlers
            for (int i = 0; i < AnswersPanel.Children.Count; i++)
            {
                if (AnswersPanel.Children[i] is Border border)
                {
                    if (border.Child is RadioButton rb)
                    {
                        // Detach
                        rb.Checked -= AnswerSelected;
                        rb.Unchecked -= AnswerSelected;
                    }
                    else if (border.Child is CheckBox cb)
                    {
                        // Detach
                        cb.Checked -= AnswerSelected;
                        cb.Unchecked -= AnswerSelected;
                    }
                }
            }

            // Set correct answers
            for (int i = 0; i < AnswersPanel.Children.Count; i++)
            {
                if (AnswersPanel.Children[i] is Border border)
                {
                    if (border.Child is RadioButton rb)
                        rb.IsChecked = correctIndices.Contains(i);
                    else if (border.Child is CheckBox cb)
                        cb.IsChecked = correctIndices.Contains(i);
                }
            }

            // Reattach event handlers
            for (int i = 0; i < AnswersPanel.Children.Count; i++)
            {
                if (AnswersPanel.Children[i] is Border border)
                {
                    if (border.Child is RadioButton rb)
                    {
                        // Reattach
                        rb.Checked += AnswerSelected;
                        rb.Unchecked += AnswerSelected;
                    }
                    else if (border.Child is CheckBox cb)
                    {
                        // Reattach
                        cb.Checked += AnswerSelected;
                        cb.Unchecked += AnswerSelected;
                    }
                }
            }

            ShowFeedback();
        }
        private async Task ShowMessageBox(string message, string title = "Quiz")
        {
            await SimpleDialog.Show(this, message, title);
        }

    }
}