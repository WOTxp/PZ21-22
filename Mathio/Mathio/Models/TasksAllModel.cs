namespace Mathio.Models;

public class TasksAllModel
{
    public TasksModel Task { get; set; }
    public IEnumerable<LessonModel> Lessons { get; set; }
    public IEnumerable<QuestionModel> Questions { get; set; }
}