using DroneDeliveryLibrary.Dtos;
using DroneDeliveryLibrary.Entities;
using DroneDeliveryLibrary.Enums;
using DroneDeliveryLibrary.Utils;
using DroneDeliveryLibrary.Validators;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DroneDeliveryLibrary.Core
{
    public class DroneDeliveriesCore
    {
        private readonly DeliveriesDataValidator _validator;

        public DroneDeliveriesCore()
        {
            _validator = new DeliveriesDataValidator();
        }

        public async Task<List<KeyValuePair<string, IEnumerable<Location>>>> CalculateDeliveries()
        {
            var data = await ExtractDeliveriesDataFromFileAsync();
            data.Item2 = RemoveLocationsExceedDroneMaxWeigth(data.Item2, data.Item1.Max(x => x.Weight));
            var parameters = CalculatePercentiles(data.Item2);
            var result = CalculateTrips(parameters, data.Item1);
            return result;
        }

        public async Task SaveDeliveriesAsFile(List<KeyValuePair<string, IEnumerable<Location>>> deliveries)
        {
            string fileName = @Path.Combine(Directory.GetCurrentDirectory(), $"deliveries_out_{DateTime.Now.ToUniversalTime().ToString().Trim().Replace(":", "_").Replace("/", "").Replace(" ", "")}.txt");
            var fileStream = new FileStream(fileName, FileMode.OpenOrCreate);
            deliveries = deliveries.OrderBy(x => x.Key).ToList();
            var distinctDrones = deliveries.Select(x => x.Key).Distinct().ToList();
            using (StreamWriter writer = new StreamWriter(fileStream, Encoding.UTF8))
            {
                foreach (var item in distinctDrones)
                {
                    var droneTrips = deliveries.Where(x => x.Key == item).ToList();
                    var numberTrip = 1;
                    await writer.WriteAsync($"[{item}]\n\r\n\r");
                    foreach (var trip in droneTrips)
                    {
                        await writer.WriteAsync($"Trip #{numberTrip}\n\r\n\r");
                        foreach (var location in trip.Value)
                        {
                            if (location.Name == trip.Value.Last().Name)
                                await writer.WriteAsync($"{location.Name}");
                            else
                                await writer.WriteAsync($"{location.Name},");
                        }
                        await writer.WriteAsync($"\n\r\n\r");
                        numberTrip++;
                    }
                }
            }
        }

        private async Task<(List<Drone>, List<Location>)> ExtractDeliveriesDataFromFileAsync()
        {
            List<Drone> droneList = new List<Drone>();
            List<Location> locationList = new List<Location>();
            int lineCount = 0;

            using (var reader = new StreamReader(Path.Combine(Directory.GetCurrentDirectory(), "deliveries.txt")))
            {
                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    var listOfStringsToRemove = "[,]".Split(',');
                    line = line.Remove(listOfStringsToRemove);

                    if (lineCount == 0)
                    {
                        _validator.ValidateMissingDeliveryData(line, DeliveryEntities.Drone);
                        var data = line.Split(',');
                        _validator.EvaluateIfIsPairDataList(data, DeliveryEntities.Drone);
                        for (int index = 0; index < data?.Length; index += 2)
                        {
                            var drone = new Drone()
                            {
                                Name = _validator.ValidateDeliveryEntityName(data[index], DeliveryEntities.Drone).TrimStart().TrimEnd(),
                                Weight = _validator.ValidateDeliveryEntityWeight(data[index + 1], DeliveryEntities.Drone)
                            };
                            droneList.Add(drone);
                        }
                    }
                    else
                    {
                        _validator.ValidateMissingDeliveryData(line, DeliveryEntities.Location);
                        var data = line.Split(',');
                        _validator.EvaluateIfIsPairDataList(data, DeliveryEntities.Location);
                        var location = new Location()
                        {
                            Name = _validator.ValidateDeliveryEntityName(data[0], DeliveryEntities.Location).TrimStart().TrimEnd(),
                            Weight = _validator.ValidateDeliveryEntityWeight(data[1], DeliveryEntities.Location)
                        };
                        locationList.Add(location);
                    }
                    lineCount++;
                }
            }

            droneList = droneList.OrderBy(x => x.Weight).ThenBy(x => x.Name).ToList();
            locationList = locationList.OrderBy(x => x.Weight).ThenBy(x => x.Name).ToList();
            return (droneList, locationList);
        }

        private List<Location> RemoveLocationsExceedDroneMaxWeigth(List<Location> locations, double droneMaxWeigth)
        {
            locations.RemoveAll(x => x.Weight > droneMaxWeigth);
            return locations.OrderBy(x => x.Weight).ThenBy(x => x.Name).ToList();
        }

        private DeliveryDataParametersDto CalculatePercentiles(List<Location> locationData)
        {
            var atypicalDataPercentage = "20";
            double leftPercentile = (double)double.Parse(atypicalDataPercentage) / 100;
            double rigthPercentile = (double)(100 - double.Parse(atypicalDataPercentage)) / 100;
            int leftPercentilePosition = (int)Math.Ceiling(leftPercentile * locationData.Count);
            int rigthPercentilePosition = (int)Math.Ceiling(rigthPercentile * locationData.Count);
            DeliveryDataParametersDto dataParameters = new DeliveryDataParametersDto();
            dataParameters.LocationAtypicalDataOnLeft = locationData.GetRange(0, leftPercentilePosition);
            dataParameters.LocationSubGroupTypicalData = locationData.GetRange(leftPercentilePosition, rigthPercentilePosition - leftPercentilePosition);
            dataParameters.LocationAtypicalDataOnRigth = locationData.GetRange(rigthPercentilePosition, locationData.Count - rigthPercentilePosition);
            return dataParameters;
        }

        private List<KeyValuePair<string, IEnumerable<Location>>> CalculateTrips(DeliveryDataParametersDto parameters, List<Drone> drones)
        {
            List<KeyValuePair<string, IEnumerable<Location>>> deliveries = new List<KeyValuePair<string, IEnumerable<Location>>>();
            if (drones != null && drones.Any())
            {
                while (ContinueTheProcess(parameters))
                {
                    List<Location> trip = new List<Location>();
                    trip = GetTripForDrone(trip, drones.Last().Weight, parameters.LocationSubGroupTypicalData);
                    trip = AdjustmentWithAtypicalValues(trip, drones.Last().Weight, parameters.LocationAtypicalDataOnLeft);
                    trip = AdjustmentWithAtypicalValues(trip, drones.Last().Weight, parameters.LocationAtypicalDataOnRigth);
                    if (trip.Any())
                    {
                        var tripWeigth = trip.Sum(x => x.Weight);
                        List<KeyValuePair<double, int>> weigthRelation = new List<KeyValuePair<double, int>>();
                        foreach (var item in drones)
                        {
                            weigthRelation.Add(new KeyValuePair<double, int>((double)tripWeigth / (double)item.Weight, drones.IndexOf(item)));
                        }
                        var weigthRelationCleaned = weigthRelation.Where(x => x.Key <= 1).ToList().OrderBy(x => x.Key).ThenBy(x => x.Value);
                        var bestDroneIndex = weigthRelationCleaned.LastOrDefault().Value;
                        var dataItem = new KeyValuePair<string, IEnumerable<Location>>(drones[bestDroneIndex].Name ?? "", trip);
                        deliveries.Add(dataItem);
                    }
                }
            }
            return deliveries;
        }

        private bool ContinueTheProcess(DeliveryDataParametersDto parameters)
        {
            if (parameters.LocationSubGroupTypicalData != null && !parameters.LocationSubGroupTypicalData.Any()
                && parameters.LocationAtypicalDataOnLeft != null && !parameters.LocationAtypicalDataOnLeft.Any()
                && parameters.LocationAtypicalDataOnRigth != null && !parameters.LocationAtypicalDataOnRigth.Any())
                return false;
            else
                return true;
        }

        //Adjustment with min o max values. Pass the rigth percentile.
        private List<Location> AdjustmentWithAtypicalValues(List<Location> trip, double droneWeigth, List<Location> LocationSubGroup)
        {
            if (LocationSubGroup != null && LocationSubGroup.Any() && trip.Sum(x => x.Weight) <= droneWeigth)
            {
                double weigthSum = trip.Sum(x => x.Weight);
                if (weigthSum + LocationSubGroup[0].Weight <= droneWeigth)
                {
                    trip.Add(LocationSubGroup[0]);
                    LocationSubGroup.Remove(LocationSubGroup[0]);
                    AdjustmentWithAtypicalValues(trip, droneWeigth, LocationSubGroup);
                }
            }
            return trip;
        }

        private List<Location> GetTripForDrone(List<Location> trip, double droneWeigth, List<Location> LocationSubGroup20To80Percentile)
        {
            if (LocationSubGroup20To80Percentile != null && LocationSubGroup20To80Percentile.Any() && trip.Sum(x => x.Weight) <= droneWeigth)
            {
                double weigthSum = trip.Sum(x => x.Weight);
                if (weigthSum + LocationSubGroup20To80Percentile[0].Weight <= droneWeigth)
                {
                    trip.Add(LocationSubGroup20To80Percentile[0]);
                    LocationSubGroup20To80Percentile.Remove(LocationSubGroup20To80Percentile[0]);
                    GetTripForDrone(trip, droneWeigth, LocationSubGroup20To80Percentile);
                }
            }
            return trip;
        }
    }
}
