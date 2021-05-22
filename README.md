[![Nuget](https://img.shields.io/nuget/v/AO.BackgroundJobs)](https://www.nuget.org/packages/AO.BackgroundJobs/)

This is a framework for working with `QueueTrigger` Azure Functions to provide highly scaleable background processing along with a comprehensive end-user notification feature, intended for Blazor.

- At the heart of this is an abstract class [BackgroundJobBase](https://github.com/adamfoneil/QueuedJobsLibrary/blob/master/QueuedJobs.Library/Abstract/BackgroundJobBase.cs) you'd use to implement the body of your Azure Function with a queue trigger. Here's a [sample](https://github.com/adamfoneil/QueuedJobsLibrary/blob/master/Testing/SampleJob.cs#L34) from the test project. See also this more [realistic zip file builder example](https://github.com/adamfoneil/QueuedJobsLibrary/blob/master/QueuedJobs.Functions/BuildZipFile.cs). As an abstract class, there a couple methods you need to implement: [OnExecuteAsync](https://github.com/adamfoneil/QueuedJobsLibrary/blob/master/QueuedJobs.Library/Abstract/BackgroundJobBase.cs#L31) and [OnStatusUpdatedAsync](https://github.com/adamfoneil/QueuedJobsLibrary/blob/master/QueuedJobs.Library/Abstract/BackgroundJobBase.cs#L36). The public method you call in your Azure Function is [ExecuteAsync](https://github.com/adamfoneil/QueuedJobsLibrary/blob/master/QueuedJobs.Library/Abstract/BackgroundJobBase.cs#L41)

- One of `BackgroundJobBase`'s dependencies is [JobRepositoryBase](https://github.com/adamfoneil/QueuedJobsLibrary/blob/master/QueuedJobs.Library/Abstract/JobRepositoryBase.cs). This is what enables the `BackgroundJobBase` to get job info and update the status of jobs in a database or wherever you're tracking job info.

- The [BackgroundJobInfo](https://github.com/adamfoneil/QueuedJobsLibrary/blob/master/QueuedJobs.Library/Models/BackgroundJobInfo.cs) model class is intended to be created as a database table. This is where this [happens](https://github.com/adamfoneil/QueuedJobsLibrary/blob/master/Testing/SampleJob.cs#L88) in a test. This is abstract so you have to create your own table with a specific `TKey` type, as in this [example](https://github.com/adamfoneil/QueuedJobsLibrary/blob/master/Testing/SampleJob.cs#L62).

- There are a couple [extension methods](https://github.com/adamfoneil/QueuedJobsLibrary/blob/master/QueuedJobs.Library/Extensions/QueueClientExtensions.cs), most importantly [QueueJobAsync](https://github.com/adamfoneil/QueuedJobsLibrary/blob/master/QueuedJobs.Library/Extensions/QueueClientExtensions.cs#L26) which you'd use from your client applications. This bundles the Azure Queue message send with storing the job info that is subsequently tracked.

On the client side, there are some pieces coming together that handle state change events and display in a Blazor app. This stuff is not working yet:

- Application has a [singleton JobTracker repository](https://github.com/adamfoneil/QueuedJobsLibrary/blob/master/Notification.Demo/Startup.cs#L28), defined [here](https://github.com/adamfoneil/QueuedJobsLibrary/blob/master/Notification.Shared/JobTrackerRepository.cs). This is what provides access to the job request and result data, start and stop timestamp and failure info. This powers both an end-user notification view as well as an admin dashboard of job info, failures, and throughput info.

- There is also a [QueueManager](https://github.com/adamfoneil/QueuedJobsLibrary/blob/master/Notification.Demo/Startup.cs#L31), defined [here](https://github.com/adamfoneil/QueuedJobsLibrary/blob/master/Notification.Demo/Services/QueueManager.cs) which provides a single access point to all the available queues in an application. For example there is a [ZipBuilder](https://github.com/adamfoneil/QueuedJobsLibrary/blob/master/Notification.Demo/Services/QueueManager.cs#L34) queue that an application can [use](https://github.com/adamfoneil/QueuedJobsLibrary/blob/master/Notification.Demo/Pages/Index.razor#L39-L43) to initiate a background job request.

- I'm using an extremely rudimentary [BackgroundJob](https://github.com/adamfoneil/QueuedJobsLibrary/blob/master/Notification.Demo/Components/BackgroundJob.razor) component at the moment that will eventually resemble something typically seen in an application under a bell icon in the upper right, for example. This is what has a little loading [spinner gif](https://github.com/adamfoneil/QueuedJobsLibrary/blob/master/Notification.Demo/Components/BackgroundJob.razor#L11), and shows the duration and, eventually, output info about the job -- such as access to a download link or some other output.

- The Blazor app must listen for job status changes, and it does so [here](https://github.com/adamfoneil/QueuedJobsLibrary/blob/master/Notification.Demo/Startup.cs#L56-L62), and then propagates this activity as an [event](https://github.com/adamfoneil/QueuedJobsLibrary/blob/master/Notification.Shared/JobTrackerRepository.cs#L60), which the `BackgroundJob` component needs to [respond to](https://github.com/adamfoneil/QueuedJobsLibrary/blob/master/Notification.Demo/Components/BackgroundJob.razor#L50).
