## Some examples of how to ...

...create an object named MyObject
```swift
object MyObject {
}
```

...add a property named SomeProperty of type bool
```swift
object MyObject {
    SomeProperty: bool
}
```

...add an optional property named OptionalProperty of type int
```swift
object MyObject {
    // explicit optional with default value
    ? OptionalProperty: int = 24

    // explicit optional without default value
    ? OptionalPropertyNoDefault: int

    // implicit optional with default value
    OptionalPropertyImplicit: int = 24
}
```

...add a property named SomeList of type List\<int\>
```swift
object MyObject {
    SomeList: List int
}
```

...add attributes to a property
```swift
object MyObject {
    // this will generate a property SomeProperty,
    // which is serialized as some_property
    SomeProperty: bool [name = some_property]

    // this will generate a property SomeList,
    // which will be ignored when serializing this object
    SomeList: List int [ignore]
}
```

...use a type which is declared in another file
```swift
object MyObject {
    SomeExternalType: ExternalType
}

// use the from attribute to specify where to get this type
external ExternalType [from="m2.external.types"]
```
