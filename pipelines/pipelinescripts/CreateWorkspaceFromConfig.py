import CreateWorkspace as cw
import json
import os

tenant_name = os.environ['TENANT_NAME']
client_id = os.environ['CLIENT_ID']
client_secret = os.environ['CLIENT_SECRET']
folder_path = os.environ['FOLDER_PATH']
environment = os.environ['ENVIRONMENT']

print(folder_path)
print(tenant_name)

def read_file(file_name):
    file = open(file_name, 'r')
    data = json.load(file)
    file.close()
    return data

# loop through files in folder
for file in os.listdir(folder_path):
    file_name = f"{folder_path}/{file}"
    data = read_file(file_name)

    workspace_name = data['workspace_name']
    capacity_name = data['capacity_name']
    workspace_description = data['workspace_description']
    users = data['users']

    # get auth header
    header = cw.get_auth_header(tenant_name, client_id, client_secret)

    # check if workspace exists
    try:
        workspace_id = cw.get_workspace_id(header, workspace_name)
        print(workspace_id)
    except:
        print('Workspace does not exist')
        # create workspace
        create_workspace = cw.create_workspace(header, workspace_name, workspace_description)
        print(create_workspace)
        workspace_id = cw.get_workspace_id(header, workspace_name)
        print(workspace_id)

    # add users to workspace
    workspace_users = cw.get_users_from_workspace(header, workspace_id)
    for user in users:
        for workspace_user in workspace_users:
            if workspace_user['identifier'].lower() == user['identifier'].lower():
                user_exists = workspace_user
            else:
                user_exists = ""
        if user_exists == "":
            try:
                cw.add_users_to_workspace(header, workspace_id, user)
            except:
                cw.add_users_to_workspace(header, workspace_id, user)
        else:
            print(f"{user['emailAddress']} exists")


    # get capacity id
    capacity_id = cw.get_capacity_id(header, capacity_name)
    print(capacity_id)


    # assign capacity to workspace
    assign_capacity = cw.assign_workspace_to_capacity(header, workspace_id, capacity_id)


    # # unassign capacity from workspace
    # unassign_capacity = cw.unassign_workspace_from_capacity(header, workspace_id)


    # #delete user from workspace
    # for user in contributor_users:
    #     user_exists = cw.get_user_from_workspace(header, workspace_id, user)
    #     if user_exists != "":
    #         print('User does exist')
    #         deleted_user = cw.delete_user_from_workspace(header, workspace_id, user)
    #         print(deleted_user)
    #     else:
    #         print('User does not exist')


    # #delete workspace
    # deleted_workspace = cw.delete_workspace(header, workspace_id)
    # print(deleted_workspace)