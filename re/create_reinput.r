# int emergenceDay,
# int ripeningDay,
# double yield,
# double clay,
# double sand,
# double layerThicknessInMillimeters,
# double percentageSoilOrganicCarbon,
# double variance,
# double alfa,
# double decompositionMinimumTemperature,
# double decompositionMaximumTemperature,
# double moistureResponseFunctionAtWiltingPoint,
# double moistureResponseFunctionAtSaturation,
# List<double> evapotranspirations,
# List<double> precipitations,
# List<double> temperatures)
here::i_am("create_reinput.r")
library(here)
library(dplyr)
library(readr)

slc = 1001009
climate_test_norms1 <- readr::read_csv(here("climateNorms_by_poly_1980_2010.csv")) %>%
	filter(SLC == slc)

climate_test_norms1_daily <- 1:365 %>%
	tibble(JulianDay = .) %>%
	rowwise() %>%
	mutate(Tavg = filter(climate_test_norms1, month==ceiling(JulianDay/30.4375))$Tavg,
			 PREC = filter(climate_test_norms1, month==ceiling(JulianDay/30.4375))$PREC,
			 PET = filter(climate_test_norms1, month==ceiling(JulianDay/30.4375))$PET) %>%
	ungroup

readr::write_csv(reinput, here("reinput.csv"))

readr::write_file(paste0(climate_test_norms1_daily$PET, collapse=","), here("reinput_pet.csv"))
readr::write_file(paste0(climate_test_norms1_daily$PREC, collapse=","), here("reinput_prec.csv"))
readr::write_file(paste0(climate_test_norms1_daily$Tavg, collapse=","), here("reinput_tavg.csv"))

