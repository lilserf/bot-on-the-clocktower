from datetime import datetime

class IDateTimeProvider:
    def now(self) -> datetime:
        pass

class DateTimeProvider(IDateTimeProvider):
    def now(self) -> datetime:
        return datetime.now()
