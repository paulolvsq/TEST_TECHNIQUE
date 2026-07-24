namespace Distributor.Application;

public interface IPresenter<T>
{
    void Display(T value);
}
