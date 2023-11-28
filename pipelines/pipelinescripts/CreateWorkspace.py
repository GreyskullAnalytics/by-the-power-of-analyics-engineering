import msal
import requests
from requests.adapters import Retry
import json


def get_auth_header(tenant_name, client_id, client_secret):
    authority_url = "https://login.microsoftonline.com/" + tenant_name
    scope = ["https://analysis.windows.net/powerbi/api/.default"]
    app = msal.ConfidentialClientApplication(client_id, authority=authority_url, client_credential=client_secret)
    result = app.acquire_token_for_client(scopes=scope)
    access_token = result['access_token']
    header = {'Content-Type':'application/json', 'Authorization':f'Bearer {access_token}'}
    return header


def get_workspace_id(header, workspace_name):
    get_group_url = "https://api.powerbi.com/v1.0/myorg/groups?$filter=name%20eq%20'" + workspace_name + "'"
    api_call = requests.get(url=get_group_url, headers=header)
    result = api_call.json()['value']
    filtered_result = ""
    for workspace in result:
        if (workspace['name'] == workspace_name):
            filtered_result = workspace
    workspace_id = filtered_result['id']
    return workspace_id


def create_workspace(header, workspace_name, workspace_description):
    create_group_url = "https://api.powerbi.com/v1.0/myorg/groups?workspaceV2=True"
    body = {
        "name": workspace_name,
        "description": workspace_description
    }
    api_call = requests.post(url=create_group_url, headers=header, json=body)
    return api_call


def add_users_to_workspace(header, workspace_id, user):
    add_users_url = f'https://api.powerbi.com/v1.0/myorg/groups/{workspace_id}/users'
    print(user)
    body = {
        "groupUserAccessRight": user['groupUserAccessRight'],
        "emailAddress": user['emailAddress'],
        "identifier": user['identifier'],
        "principalType": user['principalType']
    }
    api_call = requests.post(url=add_users_url, headers=header, json=body)
    result = api_call.json()
    print(result)
    return result


def delete_user_from_workspace(header, workspace_id, user):
    delete_users_url = f'https://api.powerbi.com/v1.0/myorg/groups/{workspace_id}/users/{user}'
    api_call = requests.delete(url=delete_users_url, headers=header)
    return api_call


def get_users_from_workspace(header, workspace_id):
    get_users_url = f'https://api.powerbi.com/v1.0/myorg/groups/{workspace_id}/users'
    api_call = requests.get(url=get_users_url, headers=header)
    result = api_call.json()['value']
    return result


def get_capacity_id(header, capacity_name):
    get_capacity_url = "https://api.powerbi.com/v1.0/myorg/capacities"
    api_call = requests.get(url=get_capacity_url, headers=header)
    result = api_call.json()['value']
    filtered_result = ""
    for capacity in result:
        if (capacity['displayName'] == capacity_name):
            filtered_result = capacity
    capacity_id = filtered_result['id']
    return capacity_id


def assign_workspace_to_capacity(header, workspace_id, capacity_id):
    assign_capacity_url = f'https://api.powerbi.com/v1.0/myorg/groups/{workspace_id}/AssignToCapacity'
    body = {
        "capacityId": capacity_id
    }
    api_call = requests.post(url=assign_capacity_url, headers=header, json=body)
    return api_call


def unassign_workspace_from_capacity(header, workspace_id):
    unassign_capacity_url = f'https://api.powerbi.com/v1.0/myorg/groups/{workspace_id}/AssignToCapacity'
    body = {
        "capacityId": "00000000-0000-0000-0000-000000000000"
    }
    api_call = requests.post(url=unassign_capacity_url, headers=header, json=body)
    return api_call


def delete_workspace(header, workspace_id):
    delete_group_url = f'https://api.powerbi.com/v1.0/myorg/groups/{workspace_id}'
    api_call = requests.delete(url=delete_group_url, headers=header)
    return api_call