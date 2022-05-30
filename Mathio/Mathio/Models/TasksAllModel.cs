namespace Mathio.Models;

public class TasksAllModel
{
    public TasksModel Task { get; set; }
    public List<LessonModel> Lessons { get; set; }
    public List<QuestionModel> Questions { get; set; }

    public TasksAllModel()
    {
        Task = new TasksModel();
        Lessons = new List<LessonModel>();
        Questions = new List<QuestionModel>();
    }
}