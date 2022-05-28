namespace Mathio.Models;

public class TestManager
{
    public TasksModel Task;
    public List<QuestionModel>? testQuestions;
    public QuestionModel? currentQuestion;
    public TestManager(TasksModel task)
    {
        Task = task;
    }

    public async Task SetupTest()
    {
        testQuestions = new List<QuestionModel>();
        await Task.DownloadAllQuestions();
        List<int> indicies = new List<int>();
        for (int i = 0; i < Task.Questions.Count; i++)
        {
            indicies.Add(i);
        }

        Random r = new Random();
        for (int i = 0; i < Task.QuestionsPerTest; i++)
        {
            int index = r.Next(indicies.Count);
            
            Console.WriteLine(index);
            Console.WriteLine(Task.Questions[index].Question);
            
            testQuestions.Add(Task.Questions[index]);
            indicies.Remove(index);
        }
    }

    public  QuestionModel? GetQuestion(int num)
    {
        if (testQuestions == null)
        {
            currentQuestion = null;
            return null;
        }
        if(num >= testQuestions.Count)
        {
            currentQuestion = null;
            return null;
        }
        currentQuestion = testQuestions[num];
        return currentQuestion;
    }
}