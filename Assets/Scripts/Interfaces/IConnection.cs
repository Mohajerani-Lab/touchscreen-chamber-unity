
public interface IConnection 
{
    public bool Connect(string adress);
    public bool CheckConnection();
    public void Send(string message);
}
