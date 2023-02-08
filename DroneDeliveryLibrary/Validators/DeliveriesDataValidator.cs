using DroneDeliveryLibrary.Entities;
using DroneDeliveryLibrary.Enums;
using DroneDeliveryLibrary.Exceptions;
using System.Collections.Generic;
using System.Linq;

namespace DroneDeliveryLibrary.Validators
{
    public class DeliveriesDataValidator
    {
        public void EvaluateIfIsPairDataList(IEnumerable<string> data, DeliveryEntities kindOfEntity)
        {
            if (data.Count() > 0 && data.Count() % 2 != 0)
                throw new FileFormatCustomException($"A {kindOfEntity}´s name or weigth is missing.");
        }

        public string ValidateDeliveryEntityName(string name, DeliveryEntities kindOfEntity)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new FileFormatCustomException($"A {kindOfEntity}´s name can´t be null, empty or white space.");
            return name;
        }

        public double ValidateDeliveryEntityWeight(string weigthInString, DeliveryEntities kindOfEntity)
        {
            if (!double.TryParse(weigthInString, out double weigth))
                throw new FileFormatCustomException($"A {kindOfEntity}´s weigth can´t be converted to double value.");
            return weigth;
        }

        public void ValidateMissingDeliveryData(string data, DeliveryEntities kindOfEntity)
        {
            if (string.IsNullOrWhiteSpace(data))
                throw new FileFormatCustomException($"{kindOfEntity}'s data is missing or a white line has been found.");
        }

        public void ValidateNullOrEmptyDeliveryData(IEnumerable<CommonDeliveryData> data, DeliveryEntities kindOfEntity)
        {
            if (data.Any(x => string.IsNullOrWhiteSpace(x.Name)))
                throw new FileFormatCustomException($"There are null or empty values in the {kindOfEntity} data.");
        }
    }
}
