# Toggl Mobile UI Tests

## Critical/basic features

- app launches
    - can login (removed with new no-user flow)
    - can start time entry
        - can stop time entry
            - can continue time entry
            - open stopped time entry details
                - edit description
            - delete stopped time entry


## More detailed basic features

- open running time entry details
- delete running time entry
- edit all bits of time entry data (this is **a lot** of different stuff)
    - start time
        - change date (should this be part of start time?)
    - duration
    - end time
        - change date (should this be possible?)
    - description
    - project
        - switch workspace
        - add project
        - select client
            - add client
        - select colour
    - tags
        - add tag
    - billable flag
    - also multiple at once


## Other features

- open side panel
    - open reports
        - go back to timer
    - open settings
        - change settings?
            - mobile tag can be tested easily, others more difficult
    - open feedback
        - send test feedback (can we verify that this works somehow automatically?)
    - log out
        - log back in


## After no-user flow changes

- test all basic features without login
- test login and sign up
    - test that data stays around after sign up (after log in?)
    - test all login-only features then

