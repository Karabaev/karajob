namespace Karajob.Web.Vacancies;

public class InboxNotifier
{
  public event Action? Changed;

  public void NotifyChanged()
  {
    Changed?.Invoke();
  }
}
