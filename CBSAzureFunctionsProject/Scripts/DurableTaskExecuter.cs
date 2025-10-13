using PlayFab.Samples;
using PlayFab.AuthenticationModels;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Newtonsoft.Json;
using CBS.Models;
using System;
using System.Threading;
using Azure.Data.Tables;
using PlayFab;
using PlayFab.CloudScriptModels;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask.Client;
using Microsoft.DurableTask;
using Microsoft.Azure.Functions.Worker.Http;

namespace CBS
{
    public class DurableTaskExecuter : BaseAzureModule
    {
        private static readonly string DurableTaskTableID = "CBSDurableTasks";

        private static readonly string RowKey = "RowKey";
        private static readonly string PartitionKey = "PartitionKey";
        private static readonly string TaskInstanceKey = "TaskInstance";
        private static readonly string ExecuteDateKey = "ExecuteDate";
        private static readonly string PartKeyValue = "EV";


        [Function(AzureFunctions.StartDurableTaskMethod)]
        public static async Task<dynamic> StartDurableTaskTrigger(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req, [DurableClient] DurableTaskClient starter)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var request = context.GetRequest<FunctionDurableTaskRequest>();
            
            var startResult = await StartDurableTaskAsync(request, starter);
            if (startResult.Error != null)
            {
                return ErrorHandler.ThrowError(startResult.Error).AsFunctionResult();
            }
            return startResult.Result.AsFunctionResult();
        }

        [Function(AzureFunctions.DurableContextProcessMethod)]
        public static async Task DurableContextProcess([OrchestrationTrigger] TaskOrchestrationContext context)
        {
            var requestContent = context.GetInput<string>();
            var request = JsonPlugin.FromJsonDecompress<FunctionDurableTaskRequest>(requestContent);
            var delay = request.Delay;
            var rawData = request.FunctionRequest;

            DateTime waitAWhile = context.CurrentUtcDateTime.Add(TimeSpan.FromSeconds(delay));
            await context.CreateTimer(waitAWhile, CancellationToken.None);
            await context.CallActivityAsync(AzureFunctions.DurabaleTaskEndMethod, requestContent);
        }

        [Function(AzureFunctions.DurabaleTaskEndMethod)]
        public static async Task<string> DurabaleTaskEnd([ActivityTrigger] string requestContent)
        {
            var request = JsonPlugin.FromJsonDecompress<FunctionDurableTaskRequest>(requestContent);
            var functionRequest = request.FunctionRequest;
            var functionName = request.FunctionName;
            var eventID = request.EventID;

            // remove task from table
            var removeResult = await CosmosTable.DeleteEntityAsync(DurableTaskTableID, eventID, string.Empty);

            // execute function
            PlayFabSettings.staticSettings.TitleId = TitleId;
            PlayFabSettings.staticSettings.DeveloperSecretKey = SercetKey;
            var entityTokenResult = await PlayFabAuthenticationAPI.GetEntityTokenAsync(new GetEntityTokenRequest());
            var entityToken = entityTokenResult.Result.EntityToken;
            var entity = entityTokenResult.Result.Entity;

            var executeRequest = new ExecuteFunctionRequest
            {
                FunctionName = functionName,
                FunctionParameter = functionRequest,
                Entity = new PlayFab.CloudScriptModels.EntityKey
                {
                    Id = entity.Id,
                    Type = entity.Type
                }
            };
            await PlayFabCloudScriptAPI.ExecuteFunctionAsync(executeRequest);
            
            return string.Empty;
        }

        public static async Task<ExecuteResult<FunctionStartDurableTaskResult>> StartDurableTaskAsync(FunctionDurableTaskRequest request, DurableTaskClient starter)
        {
            if (!request.IsValid())
            {
                return ErrorHandler.InvalidInput<FunctionStartDurableTaskResult>();
            }
            string requestRaw = JsonPlugin.ToJsonCompress(request);
            var delay = request.Delay;
            var eventID = request.EventID;
            

            // check is task is running
            var getEntityResult = await CosmosTable.GetEntityAsync(DurableTaskTableID, PartKeyValue, eventID, GetTaskEntityKeys());
            var taskEntity = getEntityResult.Result;
            if (taskEntity != null)
            {
                return ErrorHandler.TaskAlreadyRunning<FunctionStartDurableTaskResult>();
            }

            // run task
            var taskID = await starter.ScheduleNewOrchestrationInstanceAsync(AzureFunctions.DurableContextProcessMethod, requestRaw);
            var executeDate = ServerTimeUTC.AddSeconds(delay);

            // write task to table
            var entity = new TableEntity();
            entity.PartitionKey = PartKeyValue;
            entity.RowKey = eventID;
            entity[TaskInstanceKey] = taskID;
            entity[ExecuteDateKey] = executeDate;

            var upsertResult = await CosmosTable.UpsertEntityAsync(DurableTaskTableID, entity);
            if (upsertResult.Error != null)
            {
                return ErrorHandler.ThrowError<FunctionStartDurableTaskResult>(upsertResult.Error);
            }

            return new ExecuteResult<FunctionStartDurableTaskResult>
            {
                Result = new FunctionStartDurableTaskResult
                {
                    DurableTaskInstanceID = taskID,
                    ExecuteDate = executeDate,
                    EventID = eventID
                }
            };
        }

        public static async Task<ExecuteResult<string>> StopDurableTaskAsync(string instanceID, string eventID, DurableTaskClient starter)
        {
            var getEntityResult = await CosmosTable.GetEntityAsync(DurableTaskTableID, PartKeyValue, eventID, GetTaskEntityKeys());
            var taskEntity = getEntityResult.Result;
            if (taskEntity != null)
            {
                var removeResult = await CosmosTable.DeleteEntityAsync(DurableTaskTableID, eventID, PartKeyValue);
                if (removeResult.Error != null)
                {
                    return ErrorHandler.ThrowError<string>(removeResult.Error);
                }
            }

            if (!string.IsNullOrEmpty(instanceID))
            {
                try
                {
                    await starter.TerminateInstanceAsync(instanceID, "User cancel");
                }
                catch (Exception err)
                {
                    return ErrorHandler.ThrowTableError<string>(err);
                }
            }
            
            return new ExecuteResult<string>
            {
                Result = "Ok"
            };
        }

        // internal
        private static string [] GetTaskEntityKeys()
        {
            return new string [] 
            {
                RowKey,
                PartitionKey,
                TaskInstanceKey,
                ExecuteDateKey
            };
        }
    }
}