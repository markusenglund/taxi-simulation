from manim import *

DEFAULT_FONT_SIZE = 35
Text.set_default(font="sans-serif", font_size=DEFAULT_FONT_SIZE)

class ProsAndConsList(Scene):
    def construct(self):
        self.camera.background_color = "#444444"

        plus1 = MathTex("+").scale(2).to_edge(LEFT).shift(UP).set_color("#00FF00")
        pros1 = Text("Lower waiting times").next_to(plus1, RIGHT)
        plus2 = MathTex("+").scale(2).next_to(plus1, DOWN * 2).set_color("#00FF00")
        pros2 = Text("More rides allocated\nto people most in need").next_to(plus2, RIGHT)
        plus3 = MathTex("+").scale(2).next_to(plus2, DOWN * 2).set_color("#00FF00")
        pros3 = Text("Lower fares during\nlow demand").next_to(plus3, RIGHT)


        vertical_line = Line(DOWN * 3, UP * 2, color=WHITE)
        self.add(vertical_line)
        minus1 = MathTex("-").scale(2).next_to(vertical_line, RIGHT * 2).set_color("#FF0000").shift(UP * 1.5)
        con1 = Text("Fewer rides overall").next_to(minus1, RIGHT)
        minus2 = MathTex("-").scale(2).next_to(minus1, DOWN * 4).set_color("#FF0000")
        con2 = Text("Higher fares*").next_to(minus2, RIGHT)
        con2Addendum = Text("Higher fares\nduring high demand").next_to(minus2, RIGHT)

        heading = Text("Surge Pricing Pros & Cons", font_size=47).to_edge(UP)
        # pros.shift(LEFT*2)
        self.add(heading)
        self.play(FadeIn(plus1, pros1))
        self.wait(1)
        self.play(FadeIn(minus1, con1))
        self.wait(1)
        self.play(FadeIn(minus2, con2))
        self.wait(1)
        self.play(FadeIn(plus2, pros2))
        self.wait(1)
        self.play(Transform(con2, con2Addendum))
        self.wait(1)
        self.play(FadeIn(plus3, pros3))
        self.wait(3)
    
