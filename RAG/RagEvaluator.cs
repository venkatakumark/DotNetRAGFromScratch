namespace MyLlmApp.RAG;

public class RagEvaluator
{
    private readonly RagService _ragService;

private readonly ConversationHistory
    _conversationHistory;

public RagEvaluator(
    RagService ragService,
    ConversationHistory conversationHistory)
    {
        _ragService = ragService;
        _conversationHistory =
        conversationHistory;
    }
    public async Task RunAsync()
    {
        List<EvaluationTestCase> testCases =
            CreateTestCases();

        int passed = 0;
        int failed = 0;

        Console.WriteLine();
        Console.WriteLine(
            "======================================");

        Console.WriteLine(
            "           RAG EVALUATION");

        Console.WriteLine(
            "======================================");

        foreach (EvaluationTestCase test
                 in testCases)
        {
            Console.WriteLine();
            Console.WriteLine(
                "--------------------------------------");

            Console.WriteLine(
                $"Question: {test.Question}");
                _conversationHistory.Clear();
            try
{
    RagResponse response =
        await _ragService.AskAsync(
            test.Question);

    bool success =
        Evaluate(
            test,
            response);

    if (success)
    {
        passed++;

        Console.WriteLine();
        Console.WriteLine(
            "RESULT: PASS");
    }
    else
    {
        failed++;

        Console.WriteLine();
        Console.WriteLine(
            "RESULT: FAIL");
    }

    Console.WriteLine(
        $"Answer: {response.Answer}");
}
catch (Exception ex)
{
    failed++;

    Console.WriteLine();
    Console.WriteLine(
        "RESULT: ERROR");

    Console.WriteLine(
        $"Error: {ex.Message}");
}

           

            
        }

        Console.WriteLine();
        Console.WriteLine(
            "======================================");

        Console.WriteLine(
            "        EVALUATION SUMMARY");

        Console.WriteLine(
            "======================================");

        Console.WriteLine(
            $"Total Tests : {testCases.Count}");

        Console.WriteLine(
            $"Passed      : {passed}");

        Console.WriteLine(
            $"Failed      : {failed}");

        float accuracy =
            testCases.Count == 0
                ? 0
                : (float)passed /
                  testCases.Count * 100;

        Console.WriteLine(
            $"Accuracy    : {accuracy:F2}%");

        Console.WriteLine(
            "======================================");
    }

    private static bool Evaluate(
        EvaluationTestCase test,
        RagResponse response)
    {
        string answer =
            response.Answer
                .ToLowerInvariant();

        // ---------------------------------
        // Expected no-answer scenario
        // ---------------------------------

        if (test.ExpectNoAnswer)
        {
            return answer.Contains(
                "could not find relevant information")
                ||
                answer.Contains(
                    "not available");
        }

        // ---------------------------------
        // Check expected keywords
        // ---------------------------------

        foreach (string keyword
                 in test.ExpectedKeywords)
        {
            if (!answer.Contains(
                    keyword.ToLowerInvariant()))
            {
                return false;
            }
        }

        return true;
    }

    private static List<EvaluationTestCase>
        CreateTestCases()
    {
        return
        [
            new EvaluationTestCase
            {
                Question =
                    "How many days can employees work from home?",

                ExpectedKeywords =
                [
                    "two",
                    "days"
                ]
            },

            new EvaluationTestCase
            {
                Question =
                    "Who approves leave requests?",

                ExpectedKeywords =
                [
                    "manager"
                ]
            },

            new EvaluationTestCase
            {
                Question =
                    "How far in advance should employees request leave?",

                ExpectedKeywords =
                [
                    "five",
                    "working days"
                ]
            },

            new EvaluationTestCase
            {
                Question =
                    "Can confidential company information be stored on personal devices?",

                ExpectedKeywords =
                [
                    "not",
                    "personal"
                ]
            },

            new EvaluationTestCase
            {
                Question =
                    "Who should suspected security incidents be reported to?",

                ExpectedKeywords =
                [
                    "IT security team"
                ]
            },

            new EvaluationTestCase
            {
                Question =
                    "What is the maternity leave policy?",

                ExpectNoAnswer = true
            }
        ];
    }
}