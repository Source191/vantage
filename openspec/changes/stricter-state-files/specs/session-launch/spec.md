# Spec Delta

## ADDED Requirements

### Requirement: A damaged record is skipped
A record of a created worktree or a recent repository that leaves out one of its values, holds `null` for one, or names the same key twice SHALL be skipped as a corrupt record is, and every other record SHALL still be read. A record that also holds a key Torni does not know SHALL still be read.

#### Scenario: A created worktree with no branch
- **WHEN** the state folder records C:\Users\me\source\worktrees\Northwind.Orders-fix-123 with its path, repository and creation time but no branch, and records C:\Users\me\source\worktrees\Northwind.Billing-invoice-pdf in full
- **THEN** Northwind.Billing-invoice-pdf reads as created by Torni and Northwind.Orders-fix-123 does not

#### Scenario: A created worktree whose branch is null
- **WHEN** the state folder records C:\Users\me\source\worktrees\Northwind.Orders-fix-123 with `branch` of `null`
- **THEN** Northwind.Orders-fix-123 does not read as created by Torni, and Torni does not exit

#### Scenario: A recent repository with a key given twice
- **WHEN** a recent-repository record names `path` twice, and another records C:\Users\me\source\repos\Northwind.Billing
- **THEN** the wizard lists Northwind.Billing among the recent repositories and nothing from the damaged record

#### Scenario: A record holding a key Torni does not know
- **WHEN** the record of C:\Users\me\source\worktrees\Northwind.Billing-invoice-pdf holds every value and a `note` key no version of Torni writes
- **THEN** Northwind.Billing-invoice-pdf reads as created by Torni
