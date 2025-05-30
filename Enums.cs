
public enum StaffRules
{
    admin,
    boss,
    staff,
    super_staff
}

public enum Bot_command_types
{
    authentication_complete,
    enter_table_number,
    enter_name,
    enter_table_number_error,
    enter_status,
    enter_notification_necessary,
    enter_notification_unnecessary,
    enter_request_type,
    enter_request_datastart,
    enter_request_days,
    enter_reason_of_cancell_request

}

public enum Table_codes 
{
    Я,
    РВ,
    ОТ,
    К,
    ПК,
    Б,
    Т,
    ПР,
    У,
    Р,
    ОЖ

}

public enum StatusRequest 
{
    created,
    done,
    cancelled
}