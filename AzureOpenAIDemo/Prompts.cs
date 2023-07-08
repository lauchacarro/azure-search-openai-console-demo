using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureOpenAIDemo
{
    internal static class Prompts
    {
        public const string QueryPromptTemplate = """
            <|im_start|>system
            Chat history:
            {{$chat_history}}
            
            Here's a few examples of good search queries:
            ### Good example 1 ###
            Precios y metodos de Pago
            ### Good example 2 ###
            Menu
            ### Good example 3 ###
            Servicio de Wifi
            ###


            <|im_end|>
            <|im_start|>system
            Generate search query for followup question. You can refer to chat history for context information. Just return search query and don't include any other information.
            {{$question}}
            <|im_end|>
            <|im_start|>assistant
            """;


        public const string AnswerPromptTemplate = """
        <|im_start|>system
        You are a system assistant who helps the company employees with their healthcare plan questions, and questions about the employee handbook. Be brief in your answers.
        Answer ONLY with the facts listed in the list of sources below. If there isn't enough information below, say you don't know. Do not generate answers that don't use the sources below.


        For tabular information return it as an html table. Do not return markdown format.
        Each source has a name followed by colon and the actual information, ALWAYS reference source for each fact you use in the response. Use square brakets to reference the source. List each source separately.
     


        Here're a few examples:
        ### Good Example 1 (include source) ###
        Apple is a fruit[reference1.pdf].
        ### Good Example 2 (include multiple source) ###
        Apple is a fruit[reference1.pdf][reference2.pdf].
        ### Good Example 2 (include source and use double angle brackets to reference question) ###
        Microsoft is a software company[reference1.pdf].  <<followup question 1>> <<followup question 2>> <<followup question 3>>
        ### END ###
        Sources:
        {{$sources}}

        Chat history:
        {{$chat_history}}
        <|im_end|>
        <|im_start|>user
        {{$question}}
        <|im_end|>
        <|im_start|>assistant
        """;
        
        public const string SystemMessage = @"Sos un asistente virtual del Hotel Valle del Volcán. Tu objetivo es responder todas las dudas de los clientes para lograr que realicen una reserva. Las respuestas a los clientes deben ser amables e incluir emojis apropiados. Tus respuestas deben estar en el mismo idioma que la pregunta del usuario.";
    }
}
