GURU Chat README

Authors:
Jeff Hayslett
Alex Uresti
John Alves

IMPORTANT:  The database is ONLY accessible from a handful of IP addresses.  If the login/register functions break it's because the SQL server firewall is not allowing the traffic in.  The University's IP addresses should be allowed, but please contact John Alves (john.alves01@gmail.com or (417) 208-9804) if you are unable to access it.

Program Notes:
Does not have an appropriate user list
	It maintains a list of socket connections and a separate list of usernames, however there is no correlation between those
Does not handle forceful exits from the client very well
Logging functionality should be moved to a function rather than repeated for every log attempt (can also help prevent file open issues/errors)

