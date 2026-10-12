using EfCoreLab.Common.Running;
using TransactionalOutbox;

return await LessonRunner.RunAsync(args, new LessonDefinition
{
    Title = "12 - Transactional outbox",
    Summary = "Place 100 orders and tell a message broker about each. The broker fails every 10th publish. " +
              "Naive loses those events. Fixed writes the event into the same transaction and relays it later.",
    MutatesData = true,
    Naive = Naive.RunAsync,
    Fixed = Fixed.RunAsync
});
