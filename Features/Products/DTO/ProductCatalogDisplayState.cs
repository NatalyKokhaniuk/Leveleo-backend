namespace LeveLEO.Features.Products.DTO;

/// <summary>
/// Як показувати товар у UI поруч із замовленнями, відгуками, акціями (типово «видалення» адміністратором = ArchivedFromSale).
/// </summary>
public enum ProductCatalogDisplayState
{
    /// <summary>Товар активний і зазвичай видимий у публічному каталозі.</summary>
    ActiveInCatalog,

    /// <summary>Запис у БД є, але IsActive=false (знятий із продажу / «архів» після DELETE при наявності в замовленнях).</summary>
    ArchivedFromSale,

    /// <summary>Посилання на Guid без рядка в Products (малоймовірно після санітизації умов акцій).</summary>
    MissingFromDatabase
}

public static class ProductCatalogDisplayStateHelper
{
    public static ProductCatalogDisplayState Resolve(bool existsInCatalog, bool isActiveWhenPresent) =>
        !existsInCatalog
            ? ProductCatalogDisplayState.MissingFromDatabase
            : isActiveWhenPresent
                ? ProductCatalogDisplayState.ActiveInCatalog
                : ProductCatalogDisplayState.ArchivedFromSale;
}
