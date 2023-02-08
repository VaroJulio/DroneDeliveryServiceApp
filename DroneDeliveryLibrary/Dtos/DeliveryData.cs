using DroneDeliveryLibrary.Entities;
using System.Collections.Generic;

namespace DroneDeliveryLibrary.Dtos
{
    public class DeliveryData
    {
        public List<Drone> DroneList { get; set; }
        public List<Location> LocationList { get; set; }
    }
}
