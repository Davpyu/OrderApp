namespace DotNetService.Constants.Event
{
    public enum NATsEventCommon
    {
        ALL,
    }

    public enum NATsEventStatusEnum
    {
        ALL = NATsEventCommon.ALL,
        INFO, // For info or initial process
        PROCESS,
        SUCCESS,
        FAILED,
        REQUEST // For request & reply event only
    }

    public enum NATsEventActionEnum
    {
        // Common action
        ALL = NATsEventCommon.ALL,
        DEBUG,
        GET_BY_IDS,
        GET,
        CREATE,
        UPDATE,
        DELETE,
        UPDATE_STATUS,

        // Specific action
        LOGIN,
        CHECK
    }

    public enum NATsEventNATSType
    {
        CORE,
        JETSTREAM,
    }

    public enum NATsEventModuleEnum
    {
        ALL = NATsEventCommon.ALL,
        LOGGER,
        AUTH,
        USER,
        INVENTORY

        // Add more module here
    }
    
    public enum NATsEventStreamModule
    {
        JETSTREAM_PAMA,
        // Add more module here
    }
}