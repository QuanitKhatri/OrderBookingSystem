namespace Distribution.Domain.Common;

public enum UnitType
{
    Ctn,
    Box,
    Pcs,
    Bag
}

public enum MovementType
{
    Opening,
    Purchase,
    Sale,
    Damage,
    Adjustment,
    SaleReturn,
    PurchaseReturn
}

public enum PaidBy
{
    Cash,
    Bank
}

public enum ExpenseType
{
    Business,
    Self
}

public enum FoundAt
{
    Godown,
    InTransit,
    ReturnedByParty
}

public enum UserRole
{
    Admin,
    Operator,
    Viewer
}

public enum AuditAction
{
    Create,
    Update,
    Delete,
    Print
}
