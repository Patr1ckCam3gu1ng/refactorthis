namespace RefactorThis.Domain.Common.Command
{
    public interface ICommandHandler<in TCommand> where TCommand : ICommand
    {
        void Handle(TCommand model);
    }
}