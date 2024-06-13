# abb-workitem-clone

# Pipeline Specification

## Pipeline Configuration and Initialization
This pipeline enhances the setup process for customer projects within ADO by automating the collection of essential information and creating work items. Initially, it requests key details as follows:
- **Project Origin:** Define the area path for project generation, for example, "ABB Traction\Engineering Group\ECH Group."
- **Project Identification:** Input the ABB Project ID (4 digits) along with the name of the customer project, for example, "1234 MyNewCustomerProject."
- **Target Work Item ID:** Specify the ID of the "Project" work item within the "ABB Traction" ADO project, which will act as the parent for newly created work items.

Additionally, a JSON configuration file shall be provided by the user, containing vital data for action execution:
- IDs of work items to be copied from the "ABB Traction Template" ADO project.
- Node Name for each work item, such as Power, Control, or Mechanics.
- Tags for each work item, separated by semicolons.
- Fields to be updated for each work item.
- Titles for each work item.

## Automated Actions
Upon initialization, the pipeline performs the following actions:

1. **Pre-copy Validation:**
   - Verifies that the Target Work Item ID is of the work item type "Project."
   - Reviews relationships among work items in the "ABB Traction Template" to identify any non-replicable ones in the "ABB Traction" project due to missing items in the JSON file or outdated relationships.
     - Displays a warning about these relationships and requests user confirmation to proceed.
   - Confirms the existence of the specified area path in the "ABB Traction" ADO project for all new work items, based on provided Node Names and the Project Origin. The entire area path is the Project Origin + Node Name.
     - Indicates assigned Area Paths per Node Name.
     - If an Area Path is missing, it displays available areas in the target project for user selection.

2. **Template Duplication:**
   - Copies specified work items from the "ABB Traction Template" to the "ABB Traction" project as children of the Target Work Item ID.
   - Alternatively, for missing IDs, creates new "PBI" work items with details (Title, Area Path Assignment, and Product Information) from the JSON file, including a "ToBeClarified" tag to indicate the need for additional description and acceptance criteria.

3. **Title Setting:** Assigns the title of each created work item from the JSON file.

4. **Area Path Assignment:** Specifies the area path for each target work item.

5. **Tagging:**
   - Adds tags for each work item as specified.
   - In addition, a tag with the Project Identification should be applied to each work item.

6. **Field Updates:** Updates fields for each work item as specified.

7. **Target Project Name:** Sets the title of the Target Work Item ID to match the Project Identification.
