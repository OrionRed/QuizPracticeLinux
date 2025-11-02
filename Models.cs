using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace QuizPractice
{
    class Models
    {
        public class Question
        {
            [JsonIgnore]
            public string QuestionId { get; set; }
            public string Text { get; set; }
            [JsonPropertyName("QuestionType")]
            public string DataType { get; set; }

            [JsonIgnore]
            public string QuizId { get; set; }
            [JsonPropertyName("Answers")]
            public List<AnswerOption> Options { get; set; } = new();
            // Fields from JSON
            public string JsonType { get; set; }
            [JsonIgnore]
            public int? JsonId { get; set; }
            [JsonIgnore]
            public List<int> Correct { get; set; }
            // Feedback fields
            public string wpProQuiz_correct { get; set; }
            [JsonIgnore]
            public string wpProQuiz_incorrect { get; set; }
            [JsonIgnore]
            public string wpProQuiz_unattempted { get; set; }
        }
        public class AnswerOption
        {
            [JsonIgnore]
            public string Type { get; set; }
            [JsonIgnore]
            public string Name { get; set; }
            [JsonIgnore]
            public string Value { get; set; }
            public string Text { get; set; }
            public int IsCorrect { get; set; } // 0 or 1
        }
    }
}
