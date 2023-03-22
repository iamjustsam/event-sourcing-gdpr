using System.Reflection;
using System.Security.Cryptography;
using EventSourcingGdpr.Cryptoshredding.Cryptoshredding.Attributes;
using Marten;
using Marten.Services.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace EventSourcingGdpr.Cryptoshredding.Cryptoshredding.Encryption;

public class EncryptionContractResolver : JsonNetContractResolver
{
    private readonly ICryptoTransform _encryptor;
    private readonly FieldEncryptionDecryption _fieldEncryptionDecryption;

    public EncryptionContractResolver(
        ICryptoTransform encryptor,
        FieldEncryptionDecryption fieldEncryptionDecryption,
        Casing casing,
        CollectionStorage collectionStorage,
        NonPublicMembersStorage nonPublicMembersStorage = NonPublicMembersStorage.Default) 
        : base(casing, collectionStorage, nonPublicMembersStorage)
    {
        _encryptor = encryptor;
        _fieldEncryptionDecryption = fieldEncryptionDecryption;
    }

    /*protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
    {
        var properties = base.CreateProperties(type, memberSerialization);
        foreach (var jsonProperty in properties)
        {
            var isPersonalIdentifiableInforation = IsPersonalIdentifiableInformation(type, jsonProperty);
            if (isPersonalIdentifiableInforation)
            {
                var serializationJsonConverter = new EncryptionJsonConverter(_encryptor, _fieldEncryptionDecryption);
                jsonProperty.Converter = serializationJsonConverter;
            }
        }

        return properties;
    }*/

    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        var property = base.CreateProperty(member, memberSerialization);

        if (member.ShouldEncrypt())
            property.Converter = new EncryptionJsonConverter(_encryptor, _fieldEncryptionDecryption);

        return property;
    }

    private bool IsPersonalIdentifiableInformation(Type type, JsonProperty jsonProperty)
    {
        var propertyInfo = type.GetProperty(jsonProperty.UnderlyingName);
        if (propertyInfo is null)
            return false;

        var hasPersonalDataAttribute = propertyInfo.CustomAttributes.Any(x => x.AttributeType == typeof(PersonalDataAttribute));
        var propertyType = propertyInfo.PropertyType;
        var isSimpleValue = propertyType.IsValueType || propertyType == typeof(string);

        return isSimpleValue && hasPersonalDataAttribute;
    }
}
