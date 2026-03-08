namespace LeveLEO.Infrastructure.Translation.Models;

public interface ITranslatable<TTranslation> where TTranslation : ITranslation
{
    ICollection<TTranslation> Translations { get; set; }
}