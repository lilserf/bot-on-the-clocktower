import datetime
import pytimeparse
import shlex

class VoteTownInfoRetrievalVirtual:
    pass

class VoteTimerImpl:
    def __init__(self):
        pass

    async def start_timer(self, server_token, time_in_seconds):
        pass

    def get_seconds_from_string(self, str):
        return pytimeparse.parse(str)

# Concrete class for use by the Cog
class VoteTimer:
    def __init__(self):
        self.impl = VoteTimerImpl()

    async def start_timer(self, ctx):
        params = shlex.split(ctx.message.content)
        
        usage = f'Must pass a time string, e.g. "5 minutes 30 seconds" or "5m30s"'

        if len(params) < 2:
            return usage

        time_string = ' '.join(params[1:])
        time_seconds = self.impl.get_seconds_from_string(time_string)
        if not time_seconds:
            return usage

        return f'TEMP time in seconds passed: {time_seconds}'

        #return await self.impl.start_timer(server_token, self.find_role_from_message_content(ctx.message.content))

    async def stop_timer(self, ctx):
        return f'TEMP okay we should stop that timer'