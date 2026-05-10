package player

import "time"

const dailyDateLayout = "2006-01-02"

func dailyDateKey(now time.Time) string {
	return now.UTC().Format(dailyDateLayout)
}

func (service *Service) ensureDailyWindow() {
	currentDate := dailyDateKey(service.now())
	if service.dailyDate == currentDate {
		return
	}

	service.dailyDate = currentDate
	service.dailyFightCount = 0
	service.dailyStageClears = 0
	service.dailySummonCount = 0
	service.claimedDaily = map[string]bool{}
}

func (service *Service) dailyProgressFor(progressType string) int {
	switch progressType {
	case "fight":
		return service.dailyFightCount
	case "stage_clear":
		return service.dailyStageClears
	case "summon":
		return service.dailySummonCount
	default:
		return 0
	}
}
