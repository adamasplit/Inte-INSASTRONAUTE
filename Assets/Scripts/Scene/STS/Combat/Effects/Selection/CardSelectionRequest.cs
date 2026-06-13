using System.Collections.Generic;

public class CardSelectionRequest
{
    public int amount;

    public string message;

    public bool completed;

    public List<CardInstance> selectedCards =
        new();
    public System.Predicate<CardInstance> filter =
        card => true;
}