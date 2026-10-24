# Spec Delta

## ADDED Requirements

### Requirement: Deleting a leftover folder never follows links
When Vantage deletes a leftover folder from disc, every junction or symbolic link inside it SHALL be removed as a link, without descending into it or changing anything it points at, and the rest of the folder SHALL then be deleted completely, including read-only files.

#### Scenario: Leftover folder holding a junction
- **WHEN** the user force-removes an orphan folder whose node_modules holds a junction to a folder outside it, and that outside folder holds a read-only file
- **THEN** the orphan folder is gone, and the outside folder and its file still exist and the file is still read-only

#### Scenario: Leftover folder with read-only git files
- **WHEN** the user removes a broken folder whose object files git left read-only
- **THEN** the folder is deleted completely
