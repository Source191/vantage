# Spec Delta

## REMOVED Requirements

### Requirement: Descriptions are shown with their session
**Reason**: Session descriptions are removed from the product; there is nothing to show.
**Migration**: Put what a session is for in its name (the rename key changes it). The DESCRIPTION column disappears and every other column is unchanged; existing `descriptions\` files under `%LOCALAPPDATA%\Vantage` are no longer read and can be deleted.
