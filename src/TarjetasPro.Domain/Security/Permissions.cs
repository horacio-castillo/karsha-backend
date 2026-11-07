namespace TarjetasPro.Domain.Security;

public static class Permissions
{
    public const string ClaimType = "permission";

    public const string CardsCreate = "cards.create";
    public const string CardsViewOwn = "cards.view.own";
    public const string CardsEditOwn = "cards.edit.own";
    public const string CardsManageAll = "cards.manage.all";
    public const string TemplatesManage = "templates.manage";
    public const string UsersManage = "users.manage";

    public static readonly string[] All =
    [
        CardsCreate, CardsViewOwn, CardsEditOwn, CardsManageAll, TemplatesManage, UsersManage
    ];

    public static readonly IReadOnlyDictionary<string, string[]> ByRole = new Dictionary<string, string[]>
    {
        [Roles.Admin] = All,
        [Roles.Cliente] = [CardsCreate, CardsViewOwn, CardsEditOwn],
    };
}
