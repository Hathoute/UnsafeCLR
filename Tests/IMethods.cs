namespace Tests;

public interface IMethods {
    void Action();
    void ActionWithPrimitive(int param1);
    void ActionWithObject(object param1);
    void ActionWithParameters(int param1, string param2);

    string Func();
    int FuncWithPrimitive(int param1);
    string FuncWithObject(object param1);
    string FuncWithParameters(int param1, string param2);
}