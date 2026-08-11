namespace AIsle.Contracts.Population
{
    public interface IPopulationGenerator
    {
        PopulationDefinition Generate(PopulationConfig config);
    }
}
