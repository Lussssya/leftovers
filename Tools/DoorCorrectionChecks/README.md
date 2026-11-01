Run only in a disposable copy of the Unity project. Copy both C# files into
Assets (outside an Editor folder). Then launch Unity 2022.3.62f3 with
-batchmode -projectPath <copy> -executeMethod DoorCorrectionChecks.Run
-logFile <log path>. Do not pass -quit: the runtime check exits with 0 on
success and 1 on failure. It changes and renders the loaded scene in memory,
without saving those runtime changes. results.txt records the passed run.
