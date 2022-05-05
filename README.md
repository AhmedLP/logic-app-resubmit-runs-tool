# Introduction 

This tool allow to reproccess runs from a logic app with runs with a specific Status in an specific date range, for this particular case is used failed status as example.

# Description 

The following Azure management API's endpoints are used by this tool:
To get logic app runs list: (Documentation: https://docs.microsoft.com/en-us/rest/api/logic/workflow-runs/list)
- GET https://management.azure.com/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Logic/workflows/{workflowName}/runs?api-version=2016-06-01&$top={$top}&$filter={$filter}
To resubmit specific logic app run: (Documentation: https://docs.microsoft.com/en-us/rest/api/logic/workflow-trigger-histories/resubmit)
- POST https://management.azure.com/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Logic/workflows/{workflowName}/triggers/{triggerName}/histories/{historyName}/resubmit?api-version=2016-06-01

## Settings
In the poyect root appears `app.settings.json`, which are all the config variables to get configured resourses to reprocess:
Inside azureuri are all the configued variables to allow that specific logic app run can be resubmitted.

For the next example available example data for fictional resourse LA_ExampleDocumentAttachment_Test_DB.
   ```json
    {
	...
         "endpoint": {
			"getWorkflowRuns": "https://management.azure.com/subscriptions/{0}/resourceGroups/{1}/providers/Microsoft.Logic/workflows/{2}/runs?api-version=2016-06-01&$top=250&$filter=Status eq '{3}' and startTime lt {4}",
			"resubmitWorkflowTrigger": "https://management.azure.com/subscriptions/{0}/resourceGroups/{1}/providers/Microsoft.Logic/workflows/{2}/triggers/{3}/histories/{4}/resubmit?api-version=2016-06-01"
		  },

		  "azureuri": {
			"subscriptionid": "666666e6-ac66-66a6-b666-6666666666c",
			"resourcegroupname": "RG_fictional_Test_DB",
			"logicappname": "LA_ExampleDocumentAttachment_Test_DB",
			"filterstatus": "Failed",
			"startDate": "2021-06-14", Important->The start date will be always the most recent date
			"endDate": "2021-06-01", Important->The end date will be always the less recent date
			"trigger": "LA_Trigger_Failing_Scenario_Cases_Test"
		  }
    }  
   ```