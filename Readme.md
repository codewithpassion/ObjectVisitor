# Readme #

Once completed, this project will provide a visitor kind of construct to work on objects.
The base idea is to offer an infrastructure to build custom serializers/deserializers.

Instead of just using reflection, an expression tree is built and compiled into a serializer and deserializer function.
It only uses reflections once to get the properties with their getters and setters.